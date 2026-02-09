using InvestAPI.Data;
using InvestAPI.DTOs.Users;
using InvestAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace InvestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Users>> GetById(Guid id)
        {
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

        [HttpGet]
        public IActionResult GetAll()
        {

        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateUserDto dto)
        {
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            //    return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
            return CreatedAtAction(nameof(Users), new { id = todoItem.Id }, todoItem);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateUserDto dto)
        {
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
        public IActionResult Delete(Guid id)
        {

        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }
    }
}