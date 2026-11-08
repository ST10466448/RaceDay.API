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

        // GET: api/Entries/my
        [Authorize(Roles = "Participant")]
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyEntries()
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int participantId))
            {
                return Unauthorized();
            }

            var entries = await _context.Entries
                .AsNoTracking()
                .Where(e => e.ParticipantID == participantId)
                .Select(e => new
                {
                    e.EntryID,
                    e.ParticipantID,
                    e.CategoryID,
                    e.EntryDate,
                    e.EntryStatus,
                    e.RaceNumber,

                    Category = new
                    {
                        e.Category!.CategoryName,
                        e.Category.DistanceKM,
                        e.Category.EntryFee,

                        Event = new
                        {
                            e.Category.Event!.EventName,
                            e.Category.Event.EventDate,
                            e.Category.Event.Venue
                        }
                    }
                })
                .ToListAsync();

            return Ok(entries);
        }

        // GET: api/Entries
        // Retrieves entries belonging to the authenticated organiser.
        [Authorize(Roles = "Organiser")]
        [HttpGet]
        [ProducesResponseType(
            typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<object>>> GetEntries()
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
            {
                return Unauthorized();
            }

            var entries = await _context.Entries
                .AsNoTracking()
                .Where(e => e.Category != null
                    && e.Category.Event != null
                    && e.Category.Event.OrganizerID == organiserId)
                .Select(e => new
                {
                    e.EntryID,
                    e.ParticipantID,

                    ParticipantName = e.Participant != null
                        ? e.Participant.FullName
                        : "Unknown",

                    ParticipantEmail = e.Participant != null
                        ? e.Participant.Email
                        : "Unknown",

                    e.CategoryID,

                    CategoryName = e.Category!.CategoryName,
                    EventName = e.Category.Event!.EventName,

                    DistanceKM = e.Category.DistanceKM,
                    EntryFee = e.Category.EntryFee,

                    e.EntryDate,
                    e.EntryStatus,
                    e.RaceNumber
                })
                .ToListAsync();

            return Ok(entries);
        }

        // POST: api/Entries
        // Creates an entry for the authenticated participant.
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
            {
                return Forbid();
            }

            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int participantId))
            {
                return Unauthorized();
            }

            entry.ParticipantID = participantId;

            bool categoryExists = await _context.EventCategories
                .AnyAsync(c => c.CategoryID == entry.CategoryID);

            if (!categoryExists)
            {
                return BadRequest(
                    "The specified event category does not exist.");
            }

            if (entry.RaceNumber.HasValue)
            {
                bool raceNumberExists = await _context.Entries
                    .AnyAsync(e => e.RaceNumber == entry.RaceNumber);

                if (raceNumberExists)
                {
                    return Conflict(
                        "That race number is already in use.");
                }
            }

            entry.EntryDate = DateTime.UtcNow;
            entry.EntryStatus = "Confirmed";

            _context.Entries.Add(entry);
            await _context.SaveChangesAsync();

            return StatusCode(201, entry);
        }

        // PUT: api/Entries/1
        // Updates an entry belonging to the organiser.
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
            {
                return BadRequest("Entry IDs do not match.");
            }

            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
            {
                return Unauthorized();
            }

            var entry = await _context.Entries.FindAsync(id);

            if (entry == null)
            {
                return NotFound();
            }

            bool currentEntryBelongsToOrganiser =
                await _context.Entries.AnyAsync(e =>
                    e.EntryID == id &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!currentEntryBelongsToOrganiser)
            {
                return NotFound();
            }

            bool participantExists = await _context.Users
                .AnyAsync(u =>
                    u.UserID == updatedEntry.ParticipantID &&
                    u.Role == "Participant");

            if (!participantExists)
            {
                return BadRequest(
                    "The specified participant is invalid.");
            }

            bool categoryBelongsToOrganiser =
                await _context.EventCategories.AnyAsync(c =>
                    c.CategoryID == updatedEntry.CategoryID &&
                    c.Event != null &&
                    c.Event.OrganizerID == organiserId);

            if (!categoryBelongsToOrganiser)
            {
                return BadRequest(
                    "The specified category does not belong to your events.");
            }

            if (!IsValidStatus(updatedEntry.EntryStatus))
            {
                return BadRequest("Invalid entry status.");
            }

            if (updatedEntry.RaceNumber.HasValue)
            {
                bool raceNumberExists = await _context.Entries
                    .AnyAsync(e =>
                        e.RaceNumber == updatedEntry.RaceNumber &&
                        e.EntryID != id);

                if (raceNumberExists)
                {
                    return Conflict(
                        "That race number is already in use.");
                }
            }

            entry.ParticipantID = updatedEntry.ParticipantID;
            entry.CategoryID = updatedEntry.CategoryID;
            entry.EntryDate = updatedEntry.EntryDate;
            entry.EntryStatus = updatedEntry.EntryStatus;
            entry.RaceNumber = updatedEntry.RaceNumber;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Entries/1
        // Deletes an entry belonging to the organiser.
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
            {
                return Unauthorized();
            }

            var entry = await _context.Entries
                .FirstOrDefaultAsync(e => e.EntryID == id);

            if (entry == null)
            {
                return NotFound();
            }

            bool entryBelongsToOrganiser =
                await _context.Entries.AnyAsync(e =>
                    e.EntryID == id &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!entryBelongsToOrganiser)
            {
                return NotFound();
            }

            _context.Entries.Remove(entry);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Validates supported entry statuses.
        private static bool IsValidStatus(string? status)
        {
            return status is "Pending"
                or "Confirmed"
                or "Cancelled";
        }
    }
}