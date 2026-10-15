using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Entry>>> GetEntries()
        {
            return await _context.Entries
                .AsNoTracking()
                .ToListAsync();
        }

        // GET: api/Entries/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Entry>> GetEntry(int id)
        {
            var entry = await _context.Entries
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EntryID == id);

            if (entry == null)
                return NotFound();

            return entry;
        }

        // POST: api/Entries
        [HttpPost]
        public async Task<ActionResult<Entry>> CreateEntry(Entry entry)
        {
            bool participantExists = await _context.Users
                .AnyAsync(u => u.UserID == entry.ParticipantID
                    && u.Role == "Participant");

            if (!participantExists)
                return BadRequest(
                    "The specified participant does not exist or is not a participant.");

            bool categoryExists = await _context.EventCategories
                .AnyAsync(c => c.CategoryID == entry.CategoryID);

            if (!categoryExists)
                return BadRequest(
                    "The specified event category does not exist.");

            if (!IsValidStatus(entry.EntryStatus))
                return BadRequest("Invalid entry status.");

            if (entry.RaceNumber.HasValue)
            {
                bool raceNumberExists = await _context.Entries
                    .AnyAsync(e => e.RaceNumber == entry.RaceNumber);

                if (raceNumberExists)
                    return Conflict("That race number is already in use.");
            }

            _context.Entries.Add(entry);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEntry),
                new { id = entry.EntryID },
                entry);
        }

        // PUT: api/Entries/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEntry(
            int id, Entry updatedEntry)
        {
            if (id != updatedEntry.EntryID)
                return BadRequest("Entry IDs do not match.");

            var entry = await _context.Entries.FindAsync(id);

            if (entry == null)
                return NotFound();

            bool participantExists = await _context.Users
                .AnyAsync(u => u.UserID == updatedEntry.ParticipantID
                    && u.Role == "Participant");

            if (!participantExists)
                return BadRequest("The specified participant is invalid.");

            bool categoryExists = await _context.EventCategories
                .AnyAsync(c => c.CategoryID == updatedEntry.CategoryID);

            if (!categoryExists)
                return BadRequest("The specified category does not exist.");

            if (!IsValidStatus(updatedEntry.EntryStatus))
                return BadRequest("Invalid entry status.");

            if (updatedEntry.RaceNumber.HasValue)
            {
                bool raceNumberExists = await _context.Entries
                    .AnyAsync(e => e.RaceNumber == updatedEntry.RaceNumber
                        && e.EntryID != id);

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
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var entry = await _context.Entries.FindAsync(id);

            if (entry == null)
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