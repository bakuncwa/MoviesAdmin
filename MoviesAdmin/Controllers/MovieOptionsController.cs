using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Models;
using MoviesAdmin.Repositories;
using MoviesAdmin.ViewModels.Movies;

namespace MoviesAdmin.Controllers
{
    // Lets the movie form add a director or studio on the spot from its searchable pickers
    // (wwwroot/js/movies.js). Responses:
    //   201 { id, name }          created; the picker adds and selects it
    //   409 { id, name, error }   that name already exists (ignoring case and extra spaces); the
    //                             picker shows an alert and selects the existing entry instead
    //   400 { error }             the name broke a validation rule
    [Authorize(Policy = "RequireAdmin")]
    public class MovieOptionsController : Controller
    {
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

        private readonly IDirectorRepository _directorRepository;
        private readonly IStudioRepository _studioRepository;

        public MovieOptionsController(IDirectorRepository directorRepository, IStudioRepository studioRepository)
        {
            _directorRepository = directorRepository;
            _studioRepository = studioRepository;
        }

        // POST: MovieOptions/AddDirector
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDirector(NewDirectorInput input)
        {
            input.Name = Normalize(input.Name);
            ModelState.Clear();
            if (!TryValidateModel(input))
            {
                return BadRequest(new { error = FirstError() });
            }

            var existing = await _directorRepository.GetByNameAsync(input.Name);
            if (existing != null)
            {
                return Duplicate("director", existing.Id, existing.Name);
            }

            var director = new Director { Name = input.Name };
            await _directorRepository.AddAsync(director);
            return await SaveOrDuplicateAsync(
                () => _directorRepository.SaveChangesAsync(),
                async () => await _directorRepository.GetByNameAsync(input.Name) is { } d ? (d.Id, d.Name) : null,
                "director", director.Name, () => director.Id);
        }

        // POST: MovieOptions/AddStudio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudio(NewStudioInput input)
        {
            input.Name = Normalize(input.Name);
            ModelState.Clear();
            if (!TryValidateModel(input))
            {
                return BadRequest(new { error = FirstError() });
            }

            var existing = await _studioRepository.GetByNameAsync(input.Name);
            if (existing != null)
            {
                return Duplicate("studio", existing.Id, existing.Name);
            }

            var studio = new Studio { Name = input.Name };
            await _studioRepository.AddAsync(studio);
            return await SaveOrDuplicateAsync(
                () => _studioRepository.SaveChangesAsync(),
                async () => await _studioRepository.GetByNameAsync(input.Name) is { } s ? (s.Id, s.Name) : null,
                "studio", studio.Name, () => studio.Id);
        }

        // Trim and collapse inner whitespace, so "Joe  Wright " and "Joe Wright" are the same name.
        // Case is left as typed; the name lookup is case-insensitive (SQL Server's default collation).
        private static string Normalize(string? name) => Whitespace.Replace(name ?? string.Empty, " ").Trim();

        private string FirstError() =>
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "That name isn't valid.";

        private ObjectResult Duplicate(string kind, int id, string name) =>
            Conflict(new { id, name, error = $"\"{name}\" is already in the {kind} list." });

        // The unique index on Name is the final guard: if someone else added the same name between
        // the check above and this save, report it as a duplicate rather than a server error.
        private async Task<IActionResult> SaveOrDuplicateAsync(
            Func<Task<int>> save, Func<Task<(int Id, string Name)?>> findExisting, string kind, string name, Func<int> newId)
        {
            try
            {
                await save();
            }
            catch (DbUpdateException)
            {
                var existing = await findExisting();
                if (existing is { } match)
                {
                    return Duplicate(kind, match.Id, match.Name);
                }

                throw;
            }

            return StatusCode(StatusCodes.Status201Created, new { id = newId(), name });
        }
    }
}
