using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UserManagementAPI.Models;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _repo;

        public UsersController(IUserRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _repo.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user is null) return NotFound(new { error = "User not found." });
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            if (user is null) return BadRequest(new { error = "Invalid user payload." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                user.Id = Guid.NewGuid();
                await _repo.AddAsync(user);
                return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create user.", details = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] User user)
        {
            if (user is null) return BadRequest(new { error = "Invalid user payload." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != user.Id) return BadRequest(new { error = "ID mismatch." });

            try
            {
                var updated = await _repo.UpdateAsync(user);
                if (!updated) return NotFound(new { error = "User not found." });
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update user.", details = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var removed = await _repo.DeleteAsync(id);
                if (!removed) return NotFound(new { error = "User not found." });
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete user.", details = ex.Message });
            }
        }
    }
}
