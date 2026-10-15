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
    public class EntriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EntriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Entries
        [Authorize(Roles = "Organiser")]
        [HttpGet]
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

        // POST: api/Entries
        [HttpPost]
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

        // PUT: api/Entries/1
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
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

        // DELETE: api/Entries/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
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

        private static bool IsValidStatus(string? status)
        {
            return status is "Pending" or "Confirmed" or "Cancelled";
        }
    }
}