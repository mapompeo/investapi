namespace InvestAPI.Services.Quotes
{
    public interface IBrapiClient
    {
        Task<decimal?> GetPriceAsync(string ticker, CancellationToken cancellationToken = default);
    }
}
