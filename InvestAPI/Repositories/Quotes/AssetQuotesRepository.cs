using InvestAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Repositories.Quotes;

public class AssetQuotesRepository : IAssetQuotesRepository
{
    private readonly AppDbContext _context;

    public AssetQuotesRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<InvestAPI.Models.AssetQuote>> GetByTickersAsync(
        IReadOnlyCollection<string> tickers,
        CancellationToken cancellationToken = default)
    {
        if (tickers.Count == 0)
        {
            return Task.FromResult(new List<InvestAPI.Models.AssetQuote>());
        }

        return _context.AssetQuotes
            .Where(q => tickers.Contains(q.Ticker))
            .ToListAsync(cancellationToken);
    }

    public void Add(InvestAPI.Models.AssetQuote quote)
    {
        _context.AssetQuotes.Add(quote);
    }
}
