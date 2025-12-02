using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagementAPI.Models;
using UserManagementAPI.Data;
using System.Linq;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _cfg;

        private readonly AppDbContext _db;

        public AuthController(IConfiguration cfg, AppDbContext db)
        {
            _cfg = cfg;
            _db = db;
        }

        [HttpPost("login")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async System.Threading.Tasks.Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user is null) return Unauthorized(new { error = "Invalid credentials" });

            var valid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!valid) return Unauthorized(new { error = "Invalid credentials" });

            var key = _cfg.GetValue<string>("Jwt:Key") ?? "supersecret_jwt_key_please_change";
            var issuer = _cfg.GetValue<string>("Jwt:Issuer") ?? "UserManagementAPI";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.ASCII.GetBytes(key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, req.Username) }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = issuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { token = tokenString, expires = tokenDescriptor.Expires });
        }
    }
}
