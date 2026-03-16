using InvestAPI.DTOs.Auth;

namespace InvestAPI.Services.Auth;

public interface IAuthService
{
    Task<LoginResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}
