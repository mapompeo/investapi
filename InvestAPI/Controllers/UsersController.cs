using InvestAPI.Data;
using InvestAPI.DTOs.Users;
using InvestAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDto>> GetMe()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
                return NotFound();

            var response = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Users>> GetById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();
            if (currentUserId.Value != id)
                return Forbid();

            var users = await _context.Users.FindAsync(id);

            if (users == null)
                return NotFound();

            var response = new UserResponseDto
            {
                Id = users.Id,
                Name = users.Name,
                Email = users.Email,
                CreatedAt = users.CreatedAt
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> Create([FromBody] CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { message = "Email já cadastrado." });

            var user = new Users
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateUserDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();
            if (currentUserId.Value != id)
                return Forbid();

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (dto.Name != null)
                user.Name = dto.Name;
            if (dto.Email != null)
                user.Email = dto.Email;
            if (dto.Password != null)
                user.PasswordHash = HashPassword(dto.Password);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Users.AnyAsync(u => u.Id == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();
            if (currentUserId.Value != id)
                return Forbid();

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }
}