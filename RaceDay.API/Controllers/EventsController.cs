using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages race events and controls access based on user roles.
    /// Both participants and organisers can view events.
    /// Only organisers can create, update or delete their own events.
    /// </summary>
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

        /// <summary>
        /// Retrieves all available race events.
        /// </summary>
        /// <returns>A list of events.</returns>
        /// <response code="200">Returns the list of events.</response>
        /// <response code="401">The user is not authenticated.</response>
        // GET: api/Events
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Event>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _context.Events
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific race event by its ID.
        /// </summary>
        /// <param name="id">The ID of the event to retrieve.</param>
        /// <returns>The requested event.</returns>
        /// <response code="200">Returns the requested event.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="404">The event does not exist.</response>
        // GET: api/Events/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (raceEvent == null)
                return NotFound();

            return raceEvent;
        }

        /// <summary>
        /// Creates a new race event for the authenticated organiser.
        /// The organiser ID is taken from the authentication token.
        /// </summary>
        /// <param name="raceEvent">The details of the event to create.</param>
        /// <returns>The newly created event.</returns>
        /// <response code="201">The event was created successfully.</response>
        /// <response code="400">The specified route does not exist.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        // POST: api/Events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Updates an existing event owned by the authenticated organiser.
        /// </summary>
        /// <param name="id">The ID of the event to update.</param>
        /// <param name="updatedEvent">The updated event details.</param>
        /// <returns>No content if the update succeeds.</returns>
        /// <response code="204">The event was updated successfully.</response>
        /// <response code="400">The event IDs do not match.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The event does not exist or is not owned by this organiser.</response>
        // PUT: api/Events/1
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Deletes an event owned by the authenticated organiser.
        /// </summary>
        /// <param name="id">The ID of the event to delete.</param>
        /// <returns>No content if the deletion succeeds.</returns>
        /// <response code="204">The event was deleted successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The event does not exist or is not owned by this organiser.</response>
        // DELETE: api/Events/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    }
}