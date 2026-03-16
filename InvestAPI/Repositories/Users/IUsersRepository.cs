namespace InvestAPI.Repositories.Users;

public interface IUsersRepository
{
    Task<InvestAPI.Models.Users?> GetByIdAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken = default);
    Task<InvestAPI.Models.Users?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsForOtherUserAsync(string normalizedEmail, Guid excludedUserId, CancellationToken cancellationToken = default);
    void Add(InvestAPI.Models.Users user);
    void Remove(InvestAPI.Models.Users user);
}
