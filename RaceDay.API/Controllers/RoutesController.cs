using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDayRoute = RaceDay.API.Models.Route;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoutesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public RoutesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Routes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RaceDayRoute>>> GetRoutes()
        {
            return await _context.Routes
                .AsNoTracking()
                .ToListAsync();
        }

        // GET: api/Routes/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<RaceDayRoute>> GetRoute(int id)
        {
            var route = await _context.Routes
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RouteID == id);

            if (route == null)
                return NotFound();

            return route;
        }

        // POST: api/Routes
        [HttpPost]
        public async Task<ActionResult<RaceDayRoute>> CreateRoute(
            RaceDayRoute route)
        {
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetRoute),
                new { id = route.RouteID },
                route);
        }

        // PUT: api/Routes/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateRoute(
            int id, RaceDayRoute updatedRoute)
        {
            if (id != updatedRoute.RouteID)
                return BadRequest("Route IDs do not match.");

            var route = await _context.Routes.FindAsync(id);

            if (route == null)
                return NotFound();

            route.RouteName = updatedRoute.RouteName;
            route.Description = updatedRoute.Description;
            route.DistanceKM = updatedRoute.DistanceKM;
            route.StartPoint = updatedRoute.StartPoint;
            route.FinishPoint = updatedRoute.FinishPoint;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Routes/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);

            if (route == null)
                return NotFound();

            bool routeInUse = await _context.Events
                .AnyAsync(e => e.RouteID == id);

            if (routeInUse)
                return Conflict(
                    "This route is linked to an event and cannot be deleted.");

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}