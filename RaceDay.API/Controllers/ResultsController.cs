using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages race results, allowing participants to view their own results
    /// and organisers to manage results for their own events.
    /// </summary>
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

        /// <summary>
        /// Retrieves results associated with the authenticated user's entries
        /// or events, depending on their role.
        /// Participants see their own results, while organisers see results
        /// for their own events.
        /// </summary>
        /// <returns>A list of results the authenticated user is permitted to view.</returns>
        /// <response code="200">The results were retrieved successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user's role is not permitted to view results.</response>
        // GET: api/Results
        [HttpGet]
        [ProducesResponseType(
            typeof(IEnumerable<Result>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Retrieves a specific race result if the authenticated user
        /// is permitted to access it.
        /// </summary>
        /// <param name="id">The ID of the result to retrieve.</param>
        /// <returns>The requested result.</returns>
        /// <response code="200">The result was retrieved successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user's role is not permitted to view results.</response>
        /// <response code="404">The result does not exist or the user cannot access it.</response>
        // GET: api/Results/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Creates a result for an entry associated with an event
        /// owned by the authenticated organiser.
        /// Only one result may exist for each entry.
        /// </summary>
        /// <param name="result">The entry ID, finish time, position, pace, and result date.</param>
        /// <returns>The newly created result.</returns>
        /// <response code="201">The result was created successfully.</response>
        /// <response code="400">The entry does not exist or does not belong to the organiser's events.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="409">A result already exists for the entry.</response>
        // POST: api/Results
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(Result), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        /// <summary>
        /// Updates an existing result for an entry associated with an event
        /// owned by the authenticated organiser.
        /// </summary>
        /// <param name="id">The ID of the result to update.</param>
        /// <param name="updatedResult">The updated result details.</param>
        /// <returns>No content when the update succeeds.</returns>
        /// <response code="204">The result was updated successfully.</response>
        /// <response code="400">The IDs do not match or the specified entry is invalid.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The result does not exist or its current entry is not associated with the organiser's events.</response>
        /// <response code="409">Another result already exists for the specified entry.</response>
        // PUT: api/Results/1
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        /// <summary>
        /// Deletes a result associated with an event owned by the
        /// authenticated organiser.
        /// </summary>
        /// <param name="id">The ID of the result to delete.</param>
        /// <returns>No content when the result is deleted successfully.</returns>
        /// <response code="204">The result was deleted successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The result does not exist or is not associated with the organiser's events.</response>
        // DELETE: api/Results/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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