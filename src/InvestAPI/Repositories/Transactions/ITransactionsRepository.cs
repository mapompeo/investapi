namespace InvestAPI.Repositories.Transactions;

public interface ITransactionsRepository
{
    Task<List<InvestAPI.Models.Transactions>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountByAssetIdsAsync(IReadOnlyCollection<Guid> assetIds, CancellationToken cancellationToken = default);
    void Add(InvestAPI.Models.Transactions transaction);
}
