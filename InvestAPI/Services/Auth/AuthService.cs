using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvestAPI.DTOs.Auth;
using InvestAPI.Exceptions;
using InvestAPI.Models;
using InvestAPI.Repositories.Common;
using InvestAPI.Repositories.Users;
using Microsoft.IdentityModel.Tokens;
using UserEntity = InvestAPI.Models.Users;

namespace InvestAPI.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUsersRepository _usersRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(IUsersRepository usersRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _usersRepository = usersRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            throw new BadRequestException("Senha e confirmação de senha não conferem.");
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        if (await _usersRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("Email já cadastrado.");
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12)
        };

        _usersRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            Token = GenerateToken(user),
            Name = user.Name,
            Email = user.Email
        };
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _usersRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Email ou senha inválidos.");
        }

        return new LoginResponseDto
        {
            Token = GenerateToken(user),
            Name = user.Name,
            Email = user.Email
        };
    }

    private string GenerateToken(InvestAPI.Models.Users user)
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
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
