namespace InvestAPI.Services.Quotes
{
    public interface ICoinGeckoClient
    {
        Task<decimal?> GetPriceBySymbolAsync(string symbol, string vsCurrency, CancellationToken cancellationToken = default);
    }
}
