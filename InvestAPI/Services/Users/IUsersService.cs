using InvestAPI.DTOs.Users;

namespace InvestAPI.Services.Users;

public interface IUsersService
{
    Task<UserResponseDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserResponseDto> GetByIdAsync(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid currentUserId, Guid targetUserId, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default);
}
