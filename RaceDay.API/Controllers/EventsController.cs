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
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Events
        // Both roles can view events.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _context.Events
                .AsNoTracking()
                .ToListAsync();
        }

        // PUT: api/Events/1
        // Organisers can update only their own events.
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvent(
            int id, Event updatedEvent)
        {
            if (id != updatedEvent.EventID)
                return BadRequest("Event IDs do not match.");

            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.EventID == id &&
                    e.OrganizerID == organiserId);

            if (existingEvent == null)
                return NotFound();

            existingEvent.RouteID = updatedEvent.RouteID;
            existingEvent.EventName = updatedEvent.EventName;
            existingEvent.EventDate = updatedEvent.EventDate;
            existingEvent.StartTime = updatedEvent.StartTime;
            existingEvent.Venue = updatedEvent.Venue;
            existingEvent.WeatherLocation =
                updatedEvent.WeatherLocation;

            // Do not change OrganizerID from the request body.
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Events/1
        // Organisers can delete only their own events.
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.EventID == id &&
                    e.OrganizerID == organiserId);

            if (raceEvent == null)
                return NotFound();

            _context.Events.Remove(raceEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Events
        // The logged-in organiser becomes the event owner.
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event raceEvent)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            bool routeExists = await _context.Routes
                .AnyAsync(r => r.RouteID == raceEvent.RouteID);

            if (!routeExists)
                return BadRequest("The specified route does not exist.");

            // Never trust OrganizerID supplied by the request.
            raceEvent.OrganizerID = organiserId;

            _context.Events.Add(raceEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = raceEvent.EventID },
                raceEvent);
        }

        // GET: api/Events/5
        // Both roles can view an individual event.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (raceEvent == null)
                return NotFound();

            return raceEvent;
        }
    }
}