using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(
            RaceDayDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(
                u => u.Email == request.Email))
            {
                return Conflict("A user with this email already exists.");
            }

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Role = "Participant",
                DateCreated = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Created("/api/Auth/login", new
            {
                user.UserID,
                user.FullName,
                user.Email,
                user.Role,
                user.DateCreated
            });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = request.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Unauthorized("Invalid email or password.");

            var verificationResult =
                _passwordHasher.VerifyHashedPassword(
                    user, user.PasswordHash, request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid email or password.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,
                    user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var keyValue = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(keyValue))
                return StatusCode(500, "JWT signing key is not configured.");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyValue));

            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var expiryMinutes =
                _configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler()
                    .WriteToken(token),
                expiresAt = token.ValidTo,
                user = new
                {
                    user.UserID,
                    user.FullName,
                    user.Email,
                    user.Role
                }
            });
        }
    }
}