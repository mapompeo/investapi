using InvestAPI.DTOs.Users;
using InvestAPI.Exceptions;
using InvestAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Services.Users;

public class UsersService : IUsersService
{
    private readonly AppDbContext _context;

    public UsersService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserResponseDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

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

        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == targetUserId, cancellationToken);
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

            var exists = await _context.Users.AnyAsync(existingUser =>
                existingUser.Email == normalizedEmail && existingUser.Id != user.Id, cancellationToken);

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

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        EnsureOwnership(currentUserId, targetUserId);

        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == targetUserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
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
