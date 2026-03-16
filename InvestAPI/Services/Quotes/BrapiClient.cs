using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InvestAPI.Services.Quotes
{
    public class BrapiClient : IBrapiClient
    {
        private readonly HttpClient _httpClient;
        private readonly QuoteSettings _settings;

        public BrapiClient(HttpClient httpClient, IOptions<QuoteSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<decimal?> GetPriceAsync(string ticker, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return null;

            var normalizedTicker = ticker.Trim().ToUpperInvariant();
            var tokenQuery = string.IsNullOrWhiteSpace(_settings.BrapiToken)
                ? string.Empty
                : $"?token={Uri.EscapeDataString(_settings.BrapiToken)}";

            using var response = await _httpClient.GetAsync($"/api/quote/{normalizedTicker}{tokenQuery}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!json.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return null;
            if (results.GetArrayLength() == 0)
                return null;

            var first = results[0];
            if (first.TryGetProperty("regularMarketPrice", out var marketPrice) && marketPrice.TryGetDecimal(out var price))
                return price;

            if (first.TryGetProperty("regularMarketPrice", out marketPrice) && marketPrice.ValueKind == JsonValueKind.Number)
                return marketPrice.GetDecimal();

            return null;
        }
    }
}
