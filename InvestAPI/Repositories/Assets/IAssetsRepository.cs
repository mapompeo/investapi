namespace InvestAPI.Repositories.Assets;

public interface IAssetsRepository
{
    Task<List<InvestAPI.Models.Assets>> GetByUserAsync(Guid userId, bool asNoTracking, CancellationToken cancellationToken = default);
    Task<InvestAPI.Models.Assets?> GetByIdForUserAsync(Guid assetId, Guid userId, bool asNoTracking, CancellationToken cancellationToken = default);
    Task<InvestAPI.Models.Assets?> GetByTickerForUserAsync(Guid userId, string ticker, bool asNoTracking, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserAndTickerAsync(Guid userId, string ticker, CancellationToken cancellationToken = default);
    void Add(InvestAPI.Models.Assets asset);
    void Remove(InvestAPI.Models.Assets asset);
}
