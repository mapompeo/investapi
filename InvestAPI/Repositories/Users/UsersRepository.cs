using InvestAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Repositories.Users;

public class UsersRepository : IUsersRepository
{
    private readonly AppDbContext _context;

    public UsersRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<InvestAPI.Models.Users?> GetByIdAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<InvestAPI.Models.Users?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<bool> EmailExistsForOtherUserAsync(string normalizedEmail, Guid excludedUserId, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(
            u => u.Email.ToLower() == normalizedEmail && u.Id != excludedUserId,
            cancellationToken);
    }

    public void Add(InvestAPI.Models.Users user)
    {
        _context.Users.Add(user);
    }

    public void Remove(InvestAPI.Models.Users user)
    {
        _context.Users.Remove(user);
    }
}
