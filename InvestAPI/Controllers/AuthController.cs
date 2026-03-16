using InvestAPI.DTOs.Auth;
using InvestAPI.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterDto dto)
        {
            var response = await _authService.RegisterAsync(dto, HttpContext.RequestAborted);
            return Created(string.Empty, response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
        {
            var response = await _authService.LoginAsync(dto, HttpContext.RequestAborted);
            return Ok(response);
        }
    }
}