using InvestAPI.DTOs.Users;
using InvestAPI.Exceptions;
using InvestAPI.Repositories.Common;
using InvestAPI.Repositories.Users;

namespace InvestAPI.Services.Users;

public class UsersService : IUsersService
{
    private readonly IUsersRepository _usersRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UsersService(IUsersRepository usersRepository, IUnitOfWork unitOfWork)
    {
        _usersRepository = usersRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserResponseDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _usersRepository.GetByIdAsync(userId, asNoTracking: true, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        return ToDto(user.Id, user.Name, user.Email, user.CreatedAt);
    }

    public async Task<UserResponseDto> GetByIdAsync(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        EnsureOwnership(currentUserId, targetUserId);
        return await GetMeAsync(targetUserId, cancellationToken);
    }

    public async Task UpdateAsync(Guid currentUserId, Guid targetUserId, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        EnsureOwnership(currentUserId, targetUserId);

        var user = await _usersRepository.GetByIdAsync(targetUserId, asNoTracking: false, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        if (dto.Name is not null)
        {
            var normalizedName = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new BadRequestException("Nome não pode ser vazio.");
            }

            user.Name = normalizedName;
        }

        if (dto.Email is not null)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new BadRequestException("Email não pode ser vazio.");
            }

            var exists = await _usersRepository.EmailExistsForOtherUserAsync(normalizedEmail, user.Id, cancellationToken);

            if (exists)
            {
                throw new ConflictException("Email já cadastrado.");
            }

            user.Email = normalizedEmail;
        }

        if (dto.Password is not null)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        EnsureOwnership(currentUserId, targetUserId);

        var user = await _usersRepository.GetByIdAsync(targetUserId, asNoTracking: false, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        _usersRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureOwnership(Guid currentUserId, Guid targetUserId)
    {
        if (currentUserId != targetUserId)
        {
            throw new ForbiddenException();
        }
    }

    private static UserResponseDto ToDto(Guid id, string name, string email, DateTime createdAt)
    {
        return new UserResponseDto
        {
            Id = id,
            Name = name,
            Email = email,
            CreatedAt = createdAt
        };
    }
}
