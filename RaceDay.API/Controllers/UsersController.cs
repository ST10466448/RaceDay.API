using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.UserID == id)
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
        [HttpPost]
        public async Task<ActionResult> CreateUser(User user)
        {
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == user.Email);

            if (emailExists)
            {
                return Conflict("A user with this email already exists.");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.UserID },
                new
                {
                    user.UserID,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.DateCreated
                });
        }

        // PUT: api/Users/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, User updatedUser)
        {
            if (id != updatedUser.UserID)
            {
                return BadRequest(
                    "The URL ID does not match the user ID.");
            }

            var existingUser = await _context.Users.FindAsync(id);

            if (existingUser == null)
            {
                return NotFound();
            }

            bool emailExists = await _context.Users.AnyAsync(
                u => u.Email == updatedUser.Email && u.UserID != id);

            if (emailExists)
            {
                return Conflict(
                    "Another user already uses this email.");
            }

            existingUser.FullName = updatedUser.FullName;
            existingUser.Email = updatedUser.Email;
            existingUser.Role = updatedUser.Role;

            // Password changes will be handled securely
            // when authentication is implemented.

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Users/1
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