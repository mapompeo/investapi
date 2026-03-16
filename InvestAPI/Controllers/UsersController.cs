using InvestAPI.DTOs.Users;
using InvestAPI.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ApiControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDto>> GetMe()
        {
            var userId = GetCurrentUserIdOrThrow();
            var response = await _usersService.GetMeAsync(userId, HttpContext.RequestAborted);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetById(Guid id)
        {
            var currentUserId = GetCurrentUserIdOrThrow();
            var response = await _usersService.GetByIdAsync(currentUserId, id, HttpContext.RequestAborted);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateUserDto dto)
        {
            var currentUserId = GetCurrentUserIdOrThrow();
            await _usersService.UpdateAsync(currentUserId, id, dto, HttpContext.RequestAborted);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = GetCurrentUserIdOrThrow();
            await _usersService.DeleteAsync(currentUserId, id, HttpContext.RequestAborted);

            return NoContent();
        }
    }
}