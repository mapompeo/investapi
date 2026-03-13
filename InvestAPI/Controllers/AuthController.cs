using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvestAPI.Data;
using InvestAPI.DTOs.Auth;
using InvestAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InvestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { message = "Senha e confirmação de senha não conferem." });

            var normalizedEmail = dto.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
                return Conflict(new { message = "Email já cadastrado." });

            var user = new Users
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateToken(user);

            return Created(string.Empty, new LoginResponseDto
            {
                Token = token,
                Name = user.Name,
                Email = user.Email
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Email ou senha inválidos." });

            var token = GenerateToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Name = user.Name,
                Email = user.Email
            });
        }

        private string GenerateToken(Users user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado.");

            var issuer = jwtSettings["Issuer"] ?? "InvestAPI";
            var audience = jwtSettings["Audience"] ?? "InvestAPI-Users";
            var expirationInDays = int.TryParse(jwtSettings["ExpirationInDays"], out var days) ? days : 7;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("name", user.Name),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expirationInDays),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}