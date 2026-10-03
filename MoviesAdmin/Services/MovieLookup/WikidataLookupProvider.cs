using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MoviesAdmin.Services.MovieLookup
{
    // Wikidata (https://www.wikidata.org) for the structured facts, plus the lead paragraph of the
    // matching English Wikipedia article for the synopsis. Both are open data (CC0 / CC BY-SA) with
    // public APIs that need no key, so this source is always available. Posters are rare (most are
    // copyrighted) and there are no review scores. Registered as a typed HttpClient in Program.cs.
    public sealed class WikidataLookupProvider : IMovieLookupProvider
    {
        private const string WikidataApi = "https://www.wikidata.org/w/api.php";

        // "Instance of" values that mean a film: film, feature film, animated feature film,
        // television film, animated film.
        private const string FilmFilter = "haswbstatement:P31=Q11424|P31=Q24869|P31=Q29168811|P31=Q506240|P31=Q202866";

        private const string UnitedStates = "Q30";

        private static readonly Regex EntityIdPattern = new(@"^Q\d{1,12}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

        private readonly HttpClient _http;

        public WikidataLookupProvider(HttpClient http)
        {
            _http = http;
        }

        public string Key => "wikidata";

        public string DisplayName => "Wikidata + Wikipedia (open data, no key)";

        public bool IsConfigured => true;

        public bool IsValidExternalId(string externalId) => EntityIdPattern.IsMatch(externalId);

        public async Task<IReadOnlyList<MovieLookupResult>> SearchAsync(string query, int? year, CancellationToken cancellationToken)
        {
            // Full-text search restricted to films; returns item ids only.
            var search = await GetJsonAsync(
                $"{WikidataApi}?action=query&list=search&format=json&srlimit=15&srsearch={Uri.EscapeDataString($"{query} {FilmFilter}")}",
                cancellationToken);
            var ids = search.GetProperty("query").GetProperty("search").EnumerateArray()
                .Select(r => r.GetProperty("title").GetString())
                .OfType<string>()
                .ToList();
            if (ids.Count == 0)
            {
                return Array.Empty<MovieLookupResult>();
            }

            var entities = await GetEntitiesAsync(ids, "labels|claims", cancellationToken);
            return ids
                .Where(entities.ContainsKey)
                .Select(id => (id, entity: entities[id], date: ReleaseDate(entities[id])))
                .Where(e => year == null || e.date?.Year == year)
                .Take(10)
                .Select(e => new MovieLookupResult(e.id, Label(e.entity) ?? e.id, e.date?.Year, PosterUrl(e.entity, width: 120)))
                .ToList();
        }

        public async Task<MovieLookupDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
        {
            var entities = await GetEntitiesAsync(new[] { externalId }, "labels|claims|sitelinks", cancellationToken);
            if (!entities.TryGetValue(externalId, out var film))
            {
                return null;
            }

            // Director, studio, genres, and rating are references to other items; one more call
            // resolves all their English labels at once.
            var directorIds = ItemIds(film, "P57");
            var studioIds = ItemIds(film, "P272");
            var genreIds = ItemIds(film, "P136");
            var ratingIds = ItemIds(film, "P1657"); // MPA film rating; labels are "PG-13", "R", ...
            var labels = await GetLabelsAsync(directorIds.Concat(studioIds).Concat(genreIds).Concat(ratingIds).Distinct(), cancellationToken);
            string? LabelOf(string id) => labels.TryGetValue(id, out var label) ? label : null;

            var youTubeId = Strings(film, "P1651").FirstOrDefault();

            return new MovieLookupDetails
            {
                Source = DisplayName,
                ExternalId = externalId,
                SourceUrl = $"https://www.wikidata.org/wiki/{externalId}",
                Title = Label(film) ?? string.Empty,
                Synopsis = LookupText.CleanSynopsis(await WikipediaSummaryAsync(film, cancellationToken)),
                ReleaseDate = ReleaseDate(film),
                RuntimeMinutes = RuntimeMinutes(film),
                Certification = ratingIds.Select(LabelOf).FirstOrDefault(l => l != null),
                PosterUrl = PosterUrl(film, width: 500),
                TrailerUrl = youTubeId is null ? null : $"https://www.youtube.com/watch?v={youTubeId}",
                DirectorName = directorIds.Select(LabelOf).FirstOrDefault(l => l != null),
                StudioName = studioIds.Select(LabelOf).FirstOrDefault(l => l != null),
                // "science fiction film" -> "science fiction"; MovieLookupController maps aliases.
                GenreNames = genreIds.Select(LabelOf).OfType<string>()
                    .Select(g => g.EndsWith(" film", StringComparison.OrdinalIgnoreCase) ? g[..^5] : g)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        // --- Wikidata JSON helpers ------------------------------------------------------------

        private async Task<JsonElement> GetJsonAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if ((int)response.StatusCode == 429)
            {
                throw new MovieLookupException("Wikidata is rate-limiting requests. Try again in a minute.");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        }

        private async Task<Dictionary<string, JsonElement>> GetEntitiesAsync(IEnumerable<string> ids, string props, CancellationToken cancellationToken)
        {
            var json = await GetJsonAsync(
                $"{WikidataApi}?action=wbgetentities&format=json&languages=en&sitefilter=enwiki&props={props}&ids={string.Join("|", ids)}",
                cancellationToken);
            return json.TryGetProperty("entities", out var entities)
                ? entities.EnumerateObject().Where(e => !e.Value.TryGetProperty("missing", out _)).ToDictionary(e => e.Name, e => e.Value)
                : new Dictionary<string, JsonElement>();
        }

        private async Task<Dictionary<string, string>> GetLabelsAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
        {
            var idList = ids.Take(50).ToList(); // wbgetentities accepts at most 50 ids per call
            if (idList.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            var entities = await GetEntitiesAsync(idList, "labels", cancellationToken);
            return entities
                .Select(e => (e.Key, label: Label(e.Value)))
                .Where(e => e.label != null)
                .ToDictionary(e => e.Key, e => e.label!);
        }

        // Lead paragraph of the English Wikipedia article linked from the item, if there is one.
        private async Task<string?> WikipediaSummaryAsync(JsonElement film, CancellationToken cancellationToken)
        {
            if (!film.TryGetProperty("sitelinks", out var sitelinks) || !sitelinks.TryGetProperty("enwiki", out var enwiki))
            {
                return null;
            }

            var title = enwiki.GetProperty("title").GetString()!.Replace(' ', '_');
            try
            {
                var summary = await GetJsonAsync($"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(title)}", cancellationToken);
                return summary.TryGetProperty("extract", out var extract) ? extract.GetString() : null;
            }
            catch (HttpRequestException)
            {
                return null; // the rest of the details are still useful without a synopsis
            }
        }

        private static string? Label(JsonElement entity) =>
            entity.TryGetProperty("labels", out var labels) && labels.TryGetProperty("en", out var en)
                ? en.GetProperty("value").GetString()
                : null;

        // Statements for one property, best rank first and deprecated ones dropped.
        private static IEnumerable<JsonElement> Statements(JsonElement entity, string property)
        {
            if (!entity.TryGetProperty("claims", out var claims) || !claims.TryGetProperty(property, out var statements))
            {
                return Enumerable.Empty<JsonElement>();
            }

            return statements.EnumerateArray()
                .Where(s => s.GetProperty("rank").GetString() != "deprecated"
                            && s.GetProperty("mainsnak").TryGetProperty("datavalue", out _))
                .OrderByDescending(s => s.GetProperty("rank").GetString() == "preferred");
        }

        private static JsonElement Value(JsonElement statement) =>
            statement.GetProperty("mainsnak").GetProperty("datavalue").GetProperty("value");

        private static List<string> ItemIds(JsonElement entity, string property) =>
            Statements(entity, property).Select(s => Value(s).GetProperty("id").GetString()).OfType<string>().ToList();

        private static List<string> Strings(JsonElement entity, string property) =>
            Statements(entity, property).Select(s => Value(s).GetString()).OfType<string>().ToList();

        // Publication date (P577): the US release if one is recorded, otherwise the earliest
        // day-precision date. Year-only dates are skipped rather than guessed to January 1st.
        private static DateTime? ReleaseDate(JsonElement entity)
        {
            var dates = Statements(entity, "P577")
                .Select(s => (statement: s, value: Value(s)))
                .Where(d => d.value.GetProperty("precision").GetInt32() >= 11)
                .Select(d => (d.statement, date: ParseTime(d.value.GetProperty("time").GetString())))
                .Where(d => d.date != null)
                .ToList();

            var us = dates.FirstOrDefault(d => d.statement.TryGetProperty("qualifiers", out var q)
                && q.TryGetProperty("P291", out var places)
                && places.EnumerateArray().Any(p => p.TryGetProperty("datavalue", out var v) && v.GetProperty("value").GetProperty("id").GetString() == UnitedStates));

            return us.date ?? dates.Select(d => d.date).Min();
        }

        // "+2021-09-15T00:00:00Z"
        private static DateTime? ParseTime(string? time) =>
            time is { Length: > 11 } && DateTime.TryParseExact(time.Substring(1, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;

        // Duration (P2047) is a quantity with a unit: minute (Q7727), hour (Q25235), second (Q11574).
        private static int? RuntimeMinutes(JsonElement entity)
        {
            foreach (var statement in Statements(entity, "P2047"))
            {
                var value = Value(statement);
                if (!decimal.TryParse(value.GetProperty("amount").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
                {
                    continue;
                }

                var minutes = value.GetProperty("unit").GetString() switch
                {
                    "http://www.wikidata.org/entity/Q7727" => amount,
                    "http://www.wikidata.org/entity/Q25235" => amount * 60,
                    "http://www.wikidata.org/entity/Q11574" => amount / 60,
                    _ => 0
                };
                if (minutes > 0)
                {
                    return (int)Math.Round(minutes);
                }
            }

            return null;
        }

        // Film poster (P3383) or image (P18), both Wikimedia Commons file names.
        private static string? PosterUrl(JsonElement entity, int width)
        {
            var file = Strings(entity, "P3383").Concat(Strings(entity, "P18")).FirstOrDefault();
            return file is null
                ? null
                : $"https://commons.wikimedia.org/wiki/Special:FilePath/{Uri.EscapeDataString(file.Replace(' ', '_'))}?width={width}";
        }
    }
}
