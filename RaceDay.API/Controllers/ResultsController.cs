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
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Results
        // Participants see their own results.
        // Organisers see results for their own events.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Result>>> GetResults()
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Participant")
            {
                var participantResults = await _context.Results
                    .AsNoTracking()
                    .Where(r => _context.Entries.Any(e =>
                        e.EntryID == r.EntryID &&
                        e.ParticipantID == userId))
                    .ToListAsync();

                return participantResults;
            }

            if (role == "Organiser")
            {
                var organiserResults = await _context.Results
                    .AsNoTracking()
                    .Where(r => _context.Entries.Any(e =>
                        e.EntryID == r.EntryID &&
                        e.Category != null &&
                        e.Category.Event != null &&
                        e.Category.Event.OrganizerID == userId))
                    .ToListAsync();

                return organiserResults;
            }

            return Forbid();
        }

        // GET: api/Results/1
        // A participant can view only their own result.
        // An organiser can view results for their own events.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Result>> GetResult(int id)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.Results
                .AsNoTracking()
                .Where(r => r.ResultID == id);

            if (role == "Participant")
            {
                query = query.Where(r => _context.Entries.Any(e =>
                    e.EntryID == r.EntryID &&
                    e.ParticipantID == userId));
            }
            else if (role == "Organiser")
            {
                query = query.Where(r => _context.Entries.Any(e =>
                    e.EntryID == r.EntryID &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == userId));
            }
            else
            {
                return Forbid();
            }

            var result = await query.FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            return result;
        }

        // POST: api/Results
        // Only organisers can create results for their own events.
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<Result>> CreateResult(Result result)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            bool entryBelongsToOrganiser = await _context.Entries
                .AnyAsync(e =>
                    e.EntryID == result.EntryID &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!entryBelongsToOrganiser)
                return BadRequest(
                    "The entry does not exist or does not belong to your events.");

            bool resultExists = await _context.Results
                .AnyAsync(r => r.EntryID == result.EntryID);

            if (resultExists)
                return Conflict(
                    "A result already exists for this entry.");

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetResult),
                new { id = result.ResultID },
                result);
        }

        // PUT: api/Results/1
        // Only organisers can update results for their own events.
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateResult(
            int id, Result updatedResult)
        {
            if (id != updatedResult.ResultID)
                return BadRequest("Result IDs do not match.");

            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound();

            bool currentEntryBelongsToOrganiser =
                await _context.Entries.AnyAsync(e =>
                    e.EntryID == result.EntryID &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!currentEntryBelongsToOrganiser)
                return NotFound();

            bool newEntryBelongsToOrganiser =
                await _context.Entries.AnyAsync(e =>
                    e.EntryID == updatedResult.EntryID &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!newEntryBelongsToOrganiser)
                return BadRequest(
                    "The specified entry does not belong to your events.");

            bool anotherResultExists = await _context.Results
                .AnyAsync(r =>
                    r.EntryID == updatedResult.EntryID &&
                    r.ResultID != id);

            if (anotherResultExists)
                return Conflict(
                    "That entry already has another result.");

            result.EntryID = updatedResult.EntryID;
            result.FinishTime = updatedResult.FinishTime;
            result.Position = updatedResult.Position;
            result.AveragePace = updatedResult.AveragePace;
            result.ResultDate = updatedResult.ResultDate;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Results/1
        // Only organisers can delete results for their own events.
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound();

            bool entryBelongsToOrganiser = await _context.Entries
                .AnyAsync(e =>
                    e.EntryID == result.EntryID &&
                    e.Category != null &&
                    e.Category.Event != null &&
                    e.Category.Event.OrganizerID == organiserId);

            if (!entryBelongsToOrganiser)
                return NotFound();

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}