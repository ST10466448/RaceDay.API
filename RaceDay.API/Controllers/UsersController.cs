using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserID,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.DateCreated
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/Users/1
        [Authorize(Roles = "Organiser")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var currentUserId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(currentUserId, out int authenticatedUserId))
            {
                return Unauthorized();
            }

            if (id != authenticatedUserId)
            {
                return Forbid();
            }

            var user = await _context.Users
                .Where(u => u.UserID == authenticatedUserId)
                .Select(u => new
                {
                    u.UserID,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.DateCreated
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // POST: api/Users
  

        // PUT: api/Users/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateProfileRequest request)
        {
            var currentUserId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(currentUserId, out int authenticatedUserId))
            {
                return Unauthorized();
            }

            if (id != authenticatedUserId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _context.Users.FindAsync(authenticatedUserId);

            if (existingUser == null)
            {
                return NotFound();
            }

            var email = request.Email.Trim();

            bool emailExists = await _context.Users.AnyAsync(
                u => u.Email == email && u.UserID != authenticatedUserId);

            if (emailExists)
            {
                return Conflict("Another user already uses this email.");
            }

            existingUser.FullName = request.FullName.Trim();
            existingUser.Email = email;

            // The user's role and password cannot be changed here.
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Users/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            bool isOrganizer = await _context.Events
                .AnyAsync(e => e.OrganizerID == id);

            bool hasEntries = await _context.Entries
                .AnyAsync(e => e.ParticipantID == id);

            if (isOrganizer || hasEntries)
            {
                return Conflict(
                    "This user is linked to events or entries and cannot be deleted.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}