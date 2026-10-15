using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages event categories, including viewing categories and
    /// organiser-only category creation, updates, and deletion.
    /// </summary>
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

        /// <summary>
        /// Retrieves all event categories.
        /// Both participants and organisers can view categories.
        /// </summary>
        /// <returns>A list of event categories.</returns>
        /// <response code="200">The categories were retrieved successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        // GET: api/EventCategories
        [HttpGet]
        [ProducesResponseType(
            typeof(IEnumerable<EventCategory>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<EventCategory>>> GetEventCategories()
        {
            return await _context.EventCategories
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific event category by its ID.
        /// Both participants and organisers can view categories.
        /// </summary>
        /// <param name="id">The ID of the category to retrieve.</param>
        /// <returns>The requested event category.</returns>
        /// <response code="200">The category was found.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="404">The category does not exist.</response>
        // GET: api/EventCategories/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(EventCategory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventCategory>> GetEventCategory(int id)
        {
            var category = await _context.EventCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            return category;
        }

        /// <summary>
        /// Creates a category for an event owned by the authenticated organiser.
        /// Only organisers can create event categories.
        /// </summary>
        /// <param name="category">The category details to create.</param>
        /// <returns>The newly created category and its resource location.</returns>
        /// <response code="201">The category was created successfully.</response>
        /// <response code="400">The event does not exist or does not belong to the organiser.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        // POST: api/EventCategories
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(
            typeof(EventCategory), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Updates an existing category belonging to an event owned
        /// by the authenticated organiser.
        /// </summary>
        /// <param name="id">The ID of the category to update.</param>
        /// <param name="category">The updated category details.</param>
        /// <returns>No content when the update succeeds.</returns>
        /// <response code="204">The category was updated successfully.</response>
        /// <response code="400">The IDs do not match, or the target event is invalid.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The category or its current event was not found or is not owned by the organiser.</response>
        // PUT: api/EventCategories/1
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Deletes a category belonging to an event owned by the
        /// authenticated organiser, provided the category has no entries.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>No content when the category is deleted successfully.</returns>
        /// <response code="204">The category was deleted successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The category or its event was not found or is not owned by the organiser.</response>
        /// <response code="409">The category has entries and cannot be deleted.</response>
        // DELETE: api/EventCategories/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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