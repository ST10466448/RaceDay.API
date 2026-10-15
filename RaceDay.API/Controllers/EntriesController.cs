using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages participant entries in event categories.
    /// Participants can create their own entries, while organisers
    /// can view, update, and delete entries for their own events.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EntriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EntriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves entries associated with events owned by the
        /// authenticated organiser.
        /// </summary>
        /// <returns>A list of entries for the organiser's events.</returns>
        /// <response code="200">The entries were retrieved successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        // GET: api/Entries
        [Authorize(Roles = "Organiser")]
        [HttpGet]
        [ProducesResponseType(
            typeof(IEnumerable<Entry>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<Entry>>> GetEntries()
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var entries = await _context.Entries
                .AsNoTracking()
                .Where(e => e.Category != null
                    && e.Category.Event != null
                    && e.Category.Event.OrganizerID == organiserId)
                .ToListAsync();

            return entries;
        }

        /// <summary>
        /// Creates an entry for the authenticated participant.
        /// The participant ID, entry date, and initial status are
        /// assigned by the API.
        /// </summary>
        /// <param name="entry">The event category and optional race number for the entry.</param>
        /// <returns>The newly created entry.</returns>
        /// <response code="201">The entry was created successfully.</response>
        /// <response code="400">The specified category does not exist.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not a participant.</response>
        /// <response code="409">The supplied race number is already in use.</response>
        // POST: api/Entries
        [HttpPost]
        [ProducesResponseType(typeof(Entry), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Entry>> CreateEntry(Entry entry)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Participant")
                return Forbid();

            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int participantId))
                return Unauthorized();

            entry.ParticipantID = participantId;

            bool categoryExists = await _context.EventCategories
                .AnyAsync(c => c.CategoryID == entry.CategoryID);

            if (!categoryExists)
                return BadRequest(
                    "The specified event category does not exist.");

            if (entry.RaceNumber.HasValue)
            {
                bool raceNumberExists = await _context.Entries
                    .AnyAsync(e => e.RaceNumber == entry.RaceNumber);

                if (raceNumberExists)
                    return Conflict("That race number is already in use.");
            }

            entry.EntryDate = DateTime.UtcNow;
            entry.EntryStatus = "Confirmed";

            _context.Entries.Add(entry);
            await _context.SaveChangesAsync();

            return StatusCode(201, entry);
        }

        /// <summary>
        /// Updates an existing entry belonging to an event owned
        /// by the authenticated organiser.
        /// </summary>
        /// <param name="id">The ID of the entry to update.</param>
        /// <param name="updatedEntry">The updated entry details.</param>
        /// <returns>No content when the update succeeds.</returns>
        /// <response code="204">The entry was updated successfully.</response>
        /// <response code="400">The IDs, participant, category, or entry status are invalid.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The entry does not exist or is not associated with the organiser's events.</response>
        /// <response code="409">The race number is already in use.</response>
        // PUT: api/Entries/1
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateEntry(
            int id, Entry updatedEntry)
        {
            if (id != updatedEntry.EntryID)
                return BadRequest("Entry IDs do not match.");

            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var entry = await _context.Entries.FindAsync(id);

            if (entry == null)
                return NotFound();

            bool currentEntryBelongsToOrganiser =
                await _context.Entries.AnyAsync(e =>
                    e.EntryID == id &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!currentEntryBelongsToOrganiser)
                return NotFound();

            bool participantExists = await _context.Users
                .AnyAsync(u =>
                    u.UserID == updatedEntry.ParticipantID &&
                    u.Role == "Participant");

            if (!participantExists)
                return BadRequest("The specified participant is invalid.");

            bool categoryBelongsToOrganiser =
                await _context.EventCategories.AnyAsync(c =>
                    c.CategoryID == updatedEntry.CategoryID &&
                    c.Event != null &&
                    c.Event.OrganizerID == organiserId);

            if (!categoryBelongsToOrganiser)
                return BadRequest(
                    "The specified category does not belong to your events.");

            if (!IsValidStatus(updatedEntry.EntryStatus))
                return BadRequest("Invalid entry status.");

            if (updatedEntry.RaceNumber.HasValue)
            {
                bool raceNumberExists = await _context.Entries
                    .AnyAsync(e =>
                        e.RaceNumber == updatedEntry.RaceNumber &&
                        e.EntryID != id);

                if (raceNumberExists)
                    return Conflict("That race number is already in use.");
            }

            entry.ParticipantID = updatedEntry.ParticipantID;
            entry.CategoryID = updatedEntry.CategoryID;
            entry.EntryDate = updatedEntry.EntryDate;
            entry.EntryStatus = updatedEntry.EntryStatus;
            entry.RaceNumber = updatedEntry.RaceNumber;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Deletes an entry belonging to an event owned by the
        /// authenticated organiser.
        /// </summary>
        /// <param name="id">The ID of the entry to delete.</param>
        /// <returns>No content when the entry is deleted successfully.</returns>
        /// <response code="204">The entry was deleted successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The entry does not exist or is not associated with the organiser's events.</response>
        // DELETE: api/Entries/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var entry = await _context.Entries
                .FirstOrDefaultAsync(e => e.EntryID == id);

            if (entry == null)
                return NotFound();

            bool entryBelongsToOrganiser =
                await _context.Entries.AnyAsync(e =>
                    e.EntryID == id &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!entryBelongsToOrganiser)
                return NotFound();

            _context.Entries.Remove(entry);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Determines whether an entry status is supported.
        /// </summary>
        /// <param name="status">The status to validate.</param>
        /// <returns>True if the status is Pending, Confirmed, or Cancelled; otherwise false.</returns>
        private static bool IsValidStatus(string? status)
        {
            return status is "Pending" or "Confirmed" or "Cancelled";
        }
    }

}