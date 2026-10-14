
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventCategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventCategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/EventCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventCategory>>> GetEventCategories()
        {
            return await _context.EventCategories
                .AsNoTracking()
                .ToListAsync();
        }

        // GET: api/EventCategories/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EventCategory>> GetEventCategory(int id)
        {
            var category = await _context.EventCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }
        // POST: api/EventCategories
        [HttpPost]
        public async Task<ActionResult<EventCategory>> CreateEventCategory(
            EventCategory category)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventID == category.EventID);

            if (!eventExists)
            {
                return BadRequest("The specified event does not exist.");
            }

            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEventCategory),
                new { id = category.CategoryID },
                category);
        }
        // PUT: api/EventCategories/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEventCategory(
            int id, EventCategory category)
        {
            if (id != category.CategoryID)
            {
                return BadRequest(
                    "The URL ID does not match the category ID.");
            }

            var existingCategory = await _context.EventCategories
                .FindAsync(id);

            if (existingCategory == null)
            {
                return NotFound();
            }

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventID == category.EventID);

            if (!eventExists)
            {
                return BadRequest("The specified event does not exist.");
            }

            existingCategory.EventID = category.EventID;
            existingCategory.CategoryName = category.CategoryName;
            existingCategory.DistanceKM = category.DistanceKM;
            existingCategory.EntryFee = category.EntryFee;
            existingCategory.MaxParticipants = category.MaxParticipants;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/EventCategories/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEventCategory(int id)
        {
            var category = await _context.EventCategories.FindAsync(id);

            if (category == null)
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