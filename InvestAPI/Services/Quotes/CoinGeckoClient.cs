using System.Text.Json;

namespace InvestAPI.Services.Quotes
{
    public class CoinGeckoClient : ICoinGeckoClient
    {
        private readonly HttpClient _httpClient;

        public CoinGeckoClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<decimal?> GetPriceByIdAsync(string coinId, string vsCurrency, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(coinId))
                return null;

            var normalizedId = coinId.Trim().ToLowerInvariant();
            var normalizedVsCurrency = string.IsNullOrWhiteSpace(vsCurrency)
                ? "usd"
                : vsCurrency.Trim().ToLowerInvariant();

            var path = $"/api/v3/simple/price?ids={Uri.EscapeDataString(normalizedId)}&vs_currencies={Uri.EscapeDataString(normalizedVsCurrency)}";
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!json.RootElement.TryGetProperty(normalizedId, out var coinNode))
                return null;

            if (!coinNode.TryGetProperty(normalizedVsCurrency, out var priceNode) || priceNode.ValueKind != JsonValueKind.Number)
                return null;

            return priceNode.GetDecimal();
        }

        public async Task<decimal?> GetPriceBySymbolAsync(string symbol, string vsCurrency, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return null;

            var normalizedSymbol = symbol.Trim().ToLowerInvariant();
            var normalizedVsCurrency = string.IsNullOrWhiteSpace(vsCurrency)
                ? "usd"
                : vsCurrency.Trim().ToLowerInvariant();

            var path = $"/api/v3/coins/markets?vs_currency={Uri.EscapeDataString(normalizedVsCurrency)}&symbols={Uri.EscapeDataString(normalizedSymbol)}";
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (json.RootElement.ValueKind != JsonValueKind.Array || json.RootElement.GetArrayLength() == 0)
                return null;

            var first = json.RootElement[0];
            if (first.TryGetProperty("current_price", out var currentPrice) && currentPrice.TryGetDecimal(out var price))
                return price;

            if (first.TryGetProperty("current_price", out currentPrice) && currentPrice.ValueKind == JsonValueKind.Number)
                return currentPrice.GetDecimal();

            return null;
        }
    }
}
