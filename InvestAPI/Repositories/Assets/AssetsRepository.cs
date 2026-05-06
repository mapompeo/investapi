using InvestAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Repositories.Assets;

public class AssetsRepository : IAssetsRepository
{
    private readonly AppDbContext _context;

    public AssetsRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<InvestAPI.Models.Assets>> GetByUserAsync(Guid userId, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = _context.Assets.Where(a => a.UserId == userId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.ToListAsync(cancellationToken);
    }

    public Task<InvestAPI.Models.Assets?> GetByIdForUserAsync(Guid assetId, Guid userId, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = _context.Assets.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(a => a.Id == assetId && a.UserId == userId, cancellationToken);
    }

    public Task<InvestAPI.Models.Assets?> GetByTickerForUserAsync(Guid userId, string ticker, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = _context.Assets.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(a => a.UserId == userId && a.Ticker == ticker, cancellationToken);
    }

    public Task<bool> ExistsByUserAndTickerAsync(Guid userId, string ticker, CancellationToken cancellationToken = default)
    {
        return _context.Assets.AnyAsync(a => a.UserId == userId && a.Ticker == ticker, cancellationToken);
    }

    public void Add(InvestAPI.Models.Assets asset)
    {
        _context.Assets.Add(asset);
    }

    public void Remove(InvestAPI.Models.Assets asset)
    {
        _context.Assets.Remove(asset);
    }
}
