using InvestAPI.Models;

namespace InvestAPI.Services.Quotes
{
    public sealed record AssetQuoteRequest(string Ticker, AssetType Type);

    public interface IQuoteService
    {
        Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(
            IEnumerable<AssetQuoteRequest> assets,
            bool forceRefresh = false,
            CancellationToken cancellationToken = default);
    }
}
