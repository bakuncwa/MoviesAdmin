using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace MoviesAdmin.Services.ReviewText
{
    // Review text is HTML from the Quill editor on the review form (bold, italic, strikethrough,
    // lists, indentation, alignment...). It's sanitized on the way in and again on the way out, so
    // only the formatting the editor produces can ever reach a page: no scripts, styles, links,
    // images or event attributes. The 1000-character limit applies to the visible text, not the
    // markup around it.
    public static class ReviewHtml
    {
        public const int MaxTextLength = 1000;

        // HtmlSanitizer is safe to share once configured; it's only ever read from here on.
        private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

        private static readonly HtmlParser Parser = new();

        // Only Quill's own formatting survives: tags from the toolbar, plus the classes Quill uses
        // for alignment (ql-align-*) and indentation (ql-indent-1..8).
        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith(["p", "br", "strong", "em", "u", "s", "ol", "ul", "li", "blockquote", "h2", "h3"]);
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedClasses.UnionWith(["ql-align-center", "ql-align-right", "ql-align-justify"]);
            for (var level = 1; level <= 8; level++)
            {
                sanitizer.AllowedClasses.Add($"ql-indent-{level}");
            }
            return sanitizer;
        }

        // Safe HTML for storing and for Html.Raw. Reviews saved before the editor existed are plain
        // text; they pass through as text (HTML-like characters in them are encoded or dropped).
        public static string Sanitize(string? html) =>
            string.IsNullOrWhiteSpace(html) ? string.Empty : Sanitizer.Sanitize(html).Trim();

        // The characters a reader sees: tags removed, entities decoded. An editor with nothing typed
        // in it ("<p><br></p>") is 0.
        public static int VisibleLength(string? html) => PlainText(html).Length;

        public static string PlainText(string? html) =>
            string.IsNullOrWhiteSpace(html)
                ? string.Empty
                : (Parser.ParseDocument(html).Body?.TextContent ?? string.Empty).Trim();
    }
}
