namespace InvestAPI.Services.Quotes
{
    public interface ICoinGeckoClient
    {
        Task<decimal?> GetPriceByIdAsync(string coinId, string vsCurrency, CancellationToken cancellationToken = default);
        Task<decimal?> GetPriceBySymbolAsync(string symbol, string vsCurrency, CancellationToken cancellationToken = default);
    }
}
