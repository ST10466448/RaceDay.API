using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.DTOs;
using System.Security.Claims;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages user profiles and organiser-only user administration.
    /// </summary>
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

        /// <summary>
        /// Retrieves the user list without exposing password hashes.
        /// </summary>
        /// <returns>A list of user profiles.</returns>
        /// <response code="200">The user list was retrieved successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        // GET: api/Users
        [Authorize(Roles = "Organiser")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Retrieves the authenticated organiser's own profile.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>The user's profile without the password hash.</returns>
        /// <response code="200">The profile was retrieved successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The requested profile is not the authenticated user's profile.</response>
        /// <response code="404">The user does not exist.</response>
        // GET: api/Users/1
       
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUser(int id)
        {
            var currentUserId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

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

        /// <summary>
        /// Updates the authenticated user's own profile.
        /// The user's role and password cannot be changed through this endpoint.
        /// </summary>
        /// <param name="id">The ID of the profile to update.</param>
        /// <param name="request">The updated name and email address.</param>
        /// <returns>No content if the profile is updated successfully.</returns>
        /// <response code="204">The profile was updated successfully.</response>
        /// <response code="400">The supplied profile data is invalid.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The requested profile is not the authenticated user's profile.</response>
        /// <response code="404">The user does not exist.</response>
        /// <response code="409">Another user already uses the email address.</response>
        // PUT: api/Users/1
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateUser(
            int id, UpdateProfileRequest request)
        {
            var currentUserId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

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

        /// <summary>
        /// Deletes a user who is not linked to events or entries.
        /// Only an authenticated organiser can perform this operation.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>No content if the user is deleted successfully.</returns>
        /// <response code="204">The user was deleted successfully.</response>
        /// <response code="401">The user is not authenticated.</response>
        /// <response code="403">The user is not an organiser.</response>
        /// <response code="404">The user does not exist.</response>
        /// <response code="409">The user is linked to events or entries.</response>
        // DELETE: api/Users/1
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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