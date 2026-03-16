namespace InvestAPI.Repositories.Quotes;

public interface IAssetQuotesRepository
{
    Task<List<InvestAPI.Models.AssetQuote>> GetByTickersAsync(
        IReadOnlyCollection<string> tickers,
        CancellationToken cancellationToken = default);

    void Add(InvestAPI.Models.AssetQuote quote);
}
