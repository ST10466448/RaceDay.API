using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventCategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventCategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/EventCategories
        // Both roles can view categories.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventCategory>>> GetEventCategories()
        {
            return await _context.EventCategories
                .AsNoTracking()
                .ToListAsync();
        }

        // GET: api/EventCategories/1
        // Both roles can view a category.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EventCategory>> GetEventCategory(int id)
        {
            var category = await _context.EventCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            return category;
        }

        // POST: api/EventCategories
        // Organisers can create categories for their own events.
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<EventCategory>> CreateEventCategory(
            EventCategory category)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            bool eventBelongsToOrganiser = await _context.Events
                .AnyAsync(e =>
                    e.EventID == category.EventID &&
                    e.OrganizerID == organiserId);

            if (!eventBelongsToOrganiser)
                return BadRequest(
                    "The specified event does not exist or does not belong to you.");

            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEventCategory),
                new { id = category.CategoryID },
                category);
        }

        // PUT: api/EventCategories/1
        // Organisers can update categories for their own events.
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEventCategory(
            int id, EventCategory category)
        {
            if (id != category.CategoryID)
                return BadRequest(
                    "The URL ID does not match the category ID.");

            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var existingCategory = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (existingCategory == null)
                return NotFound();

            bool currentEventBelongsToOrganiser =
                await _context.Events.AnyAsync(e =>
                    e.EventID == existingCategory.EventID &&
                    e.OrganizerID == organiserId);

            if (!currentEventBelongsToOrganiser)
                return NotFound();

            bool newEventBelongsToOrganiser =
                await _context.Events.AnyAsync(e =>
                    e.EventID == category.EventID &&
                    e.OrganizerID == organiserId);

            if (!newEventBelongsToOrganiser)
                return BadRequest(
                    "The specified event does not exist or does not belong to you.");

            existingCategory.EventID = category.EventID;
            existingCategory.CategoryName = category.CategoryName;
            existingCategory.DistanceKM = category.DistanceKM;
            existingCategory.EntryFee = category.EntryFee;
            existingCategory.MaxParticipants = category.MaxParticipants;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/EventCategories/1
        // Organisers can delete categories for their own events.
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEventCategory(int id)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var category = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            bool eventBelongsToOrganiser = await _context.Events
                .AnyAsync(e =>
                    e.EventID == category.EventID &&
                    e.OrganizerID == organiserId);

            if (!eventBelongsToOrganiser)
                return NotFound();

            bool categoryInUse = await _context.Entries
                .AnyAsync(e => e.CategoryID == id);

            if (categoryInUse)
                return Conflict(
                    "This category has entries and cannot be deleted.");

            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}