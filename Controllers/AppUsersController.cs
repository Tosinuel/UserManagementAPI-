using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UserManagementAPI.Data;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class AppUsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AppUsersController> _logger;

        public AppUsersController(AppDbContext db, ILogger<AppUsersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.AppUsers.AsNoTracking().Select(u => new { u.Id, u.Username, u.Role }).ToListAsync();
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppUserRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _db.AppUsers.AnyAsync(u => u.Username == req.Username))
                return Conflict(new { error = "Username already exists." });

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Username = req.Username,
                Role = req.Role ?? "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
            };

            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Admin {Admin} created AppUser {Username}", User?.Identity?.Name, req.Username);

            return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new { user.Id, user.Username, user.Role });
        }

        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _db.AppUsers.FindAsync(id);
            if (user is null) return NotFound(new { error = "AppUser not found." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Admin {Admin} reset password for AppUser {Username}", User?.Identity?.Name, user.Username);

            return NoContent();
        }
    }

    public class CreateAppUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
