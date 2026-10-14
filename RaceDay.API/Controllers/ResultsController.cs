using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Results
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Result>>> GetResults()
        {
            return await _context.Results
                .AsNoTracking()
                .ToListAsync();
        }

        // GET: api/Results/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Result>> GetResult(int id)
        {
            var result = await _context.Results
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound();

            return result;
        }

        // POST: api/Results
        [HttpPost]
        public async Task<ActionResult<Result>> CreateResult(Result result)
        {
            bool entryExists = await _context.Entries
                .AnyAsync(e => e.EntryID == result.EntryID);

            if (!entryExists)
                return BadRequest("The specified entry does not exist.");

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
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateResult(
            int id, Result updatedResult)
        {
            if (id != updatedResult.ResultID)
                return BadRequest("Result IDs do not match.");

            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound();

            bool entryExists = await _context.Entries
                .AnyAsync(e => e.EntryID == updatedResult.EntryID);

            if (!entryExists)
                return BadRequest("The specified entry does not exist.");

            bool anotherResultExists = await _context.Results
                .AnyAsync(r => r.EntryID == updatedResult.EntryID
                    && r.ResultID != id);

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
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound();

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}