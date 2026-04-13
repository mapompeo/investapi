using InvestAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Repositories.Transactions;

public class TransactionsRepository : ITransactionsRepository
{
    private readonly AppDbContext _context;

    public TransactionsRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<InvestAPI.Models.Transactions>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Transactions
            .AsNoTracking()
            .Include(t => t.Assets)
            .Where(t => t.Assets.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByAssetIdsAsync(IReadOnlyCollection<Guid> assetIds, CancellationToken cancellationToken = default)
    {
        if (assetIds.Count == 0)
        {
            return Task.FromResult(0);
        }

        return _context.Transactions
            .AsNoTracking()
            .CountAsync(t => assetIds.Contains(t.AssetId), cancellationToken);
    }

    public void Add(InvestAPI.Models.Transactions transaction)
    {
        _context.Transactions.Add(transaction);
    }
}
