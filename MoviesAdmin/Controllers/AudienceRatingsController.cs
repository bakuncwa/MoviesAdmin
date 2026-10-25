using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesAdmin.Data;
using MoviesAdmin.Models;

namespace MoviesAdmin.Controllers
{
    // Scaffolded CRUD for AudienceRating (one-to-many with Movie: an audience rating can have many movies, and a movie belongs to one audience rating).
    public class AudienceRatingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AudienceRatingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AudienceRatings
        public async Task<IActionResult> Index()
        {
            // Movies included so the list can show how many movies use each audience rating.
            return View(await _context.AudienceRatings.Include(x => x.Movies).OrderBy(x => x.Id).ToListAsync());
        }

        // GET: AudienceRatings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var audienceRating = await _context.AudienceRatings
                .Include(x => x.Movies)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (audienceRating == null)
            {
                return NotFound();
            }

            return View(audienceRating);
        }

        // GET: AudienceRatings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AudienceRatings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] AudienceRating audienceRating)
        {
            if (ModelState.IsValid)
            {
                _context.Add(audienceRating);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(audienceRating);
        }

        // GET: AudienceRatings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var audienceRating = await _context.AudienceRatings.FindAsync(id);
            if (audienceRating == null)
            {
                return NotFound();
            }
            return View(audienceRating);
        }

        // POST: AudienceRatings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] AudienceRating audienceRating)
        {
            if (id != audienceRating.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(audienceRating);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AudienceRatingExists(audienceRating.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(audienceRating);
        }

        // GET: AudienceRatings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var audienceRating = await _context.AudienceRatings
                .Include(x => x.Movies)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (audienceRating == null)
            {
                return NotFound();
            }

            return View(audienceRating);
        }

        // POST: AudienceRatings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var audienceRating = await _context.AudienceRatings.FindAsync(id);
            if (audienceRating != null)
            {
                // Movie.AudienceRatingId is required and the FK is Restrict, so an audience rating still used by
                // movies can't be deleted: show the Delete page again with a message instead.
                if (await _context.Movies.AnyAsync(m => m.AudienceRatingId == id))
                {
                    ModelState.AddModelError(string.Empty, "This audience rating still has movies. Change their audience rating first, then delete it.");
                    await _context.Entry(audienceRating).Collection(x => x.Movies).LoadAsync();
                    return View(audienceRating);
                }

                _context.AudienceRatings.Remove(audienceRating);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AudienceRatingExists(int id)
        {
            return _context.AudienceRatings.Any(e => e.Id == id);
        }
    }
}
