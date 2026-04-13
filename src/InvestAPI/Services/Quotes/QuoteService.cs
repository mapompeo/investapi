using InvestAPI.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace InvestAPI.Services.Quotes;

public class QuoteService : IQuoteService
{
    private readonly IBrapiClient _brapiClient;
    private readonly ICoinGeckoClient _coinGeckoClient;
    private readonly QuoteSettings _settings;

    public QuoteService(
        IBrapiClient brapiClient,
        ICoinGeckoClient coinGeckoClient,
        IOptions<QuoteSettings> options)
    {
        _brapiClient = brapiClient;
        _coinGeckoClient = coinGeckoClient;
        _settings = options.Value;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(
        IEnumerable<AssetQuoteRequest> assets,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var requestedAssets = assets
            .Where(asset => !string.IsNullOrWhiteSpace(asset.Ticker))
            .Select(asset => new AssetQuoteRequest(asset.Ticker.Trim().ToUpperInvariant(), asset.Type))
            .GroupBy(asset => asset.Ticker)
            .Select(group => group.First())
            .ToList();

        if (requestedAssets.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in requestedAssets)
        {
            decimal? price;

            try
            {
                if (asset.Type == AssetType.Crypto)
                {
                    var coinId = ResolveCoinGeckoId(asset.Ticker);

                    price = !string.IsNullOrWhiteSpace(coinId)
                        ? await _coinGeckoClient.GetPriceByIdAsync(coinId, _settings.CoinGeckoVsCurrency, cancellationToken)
                        : null;

                    price ??= await _coinGeckoClient.GetPriceBySymbolAsync(asset.Ticker, _settings.CoinGeckoVsCurrency, cancellationToken);
                }
                else
                {
                    price = await _brapiClient.GetPriceAsync(asset.Ticker, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                price = null;
            }
            catch (HttpRequestException)
            {
                price = null;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                price = null;
            }
            catch (JsonException)
            {
                price = null;
            }

            if (price is null || price <= 0)
            {
                continue;
            }

            result[asset.Ticker] = price.Value;
        }

        return result;
    }

    private string? ResolveCoinGeckoId(string ticker)
    {
        if (_settings.CoinGeckoTickerToId.TryGetValue(ticker, out var mappedId) && !string.IsNullOrWhiteSpace(mappedId))
        {
            return mappedId;
        }

        return ticker.Trim().ToLowerInvariant();
    }
}