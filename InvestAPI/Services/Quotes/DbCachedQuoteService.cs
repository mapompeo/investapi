using InvestAPI.Models;
using InvestAPI.Repositories.Common;
using InvestAPI.Repositories.Quotes;
using Microsoft.Extensions.Options;

namespace InvestAPI.Services.Quotes
{
    public class DbCachedQuoteService : IQuoteService
    {
        private readonly IAssetQuotesRepository _assetQuotesRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBrapiClient _brapiClient;
        private readonly ICoinGeckoClient _coinGeckoClient;
        private readonly QuoteSettings _settings;
        private readonly ILogger<DbCachedQuoteService> _logger;

        public DbCachedQuoteService(
            IAssetQuotesRepository assetQuotesRepository,
            IUnitOfWork unitOfWork,
            IBrapiClient brapiClient,
            ICoinGeckoClient coinGeckoClient,
            IOptions<QuoteSettings> options,
            ILogger<DbCachedQuoteService> logger)
        {
            _assetQuotesRepository = assetQuotesRepository;
            _unitOfWork = unitOfWork;
            _brapiClient = brapiClient;
            _coinGeckoClient = coinGeckoClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(
            IEnumerable<AssetQuoteRequest> assets,
            bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            var requestedAssets = assets
                .Where(a => !string.IsNullOrWhiteSpace(a.Ticker))
                .Select(a => new AssetQuoteRequest(a.Ticker.Trim().ToUpperInvariant(), a.Type))
                .GroupBy(a => a.Ticker)
                .Select(g => g.First())
                .ToList();

            if (requestedAssets.Count == 0)
                return new Dictionary<string, decimal>();

            var tickers = requestedAssets.Select(a => a.Ticker).ToList();
            var now = DateTime.UtcNow;
            var cacheWindowStart = now.AddMinutes(-Math.Max(1, _settings.CacheMinutes));

            var existingQuotes = await _assetQuotesRepository.GetByTickersAsync(tickers, cancellationToken);

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var existing in existingQuotes.Where(q => !forceRefresh && q.LastUpdate >= cacheWindowStart && q.CurrentPrice > 0))
                result[existing.Ticker] = existing.CurrentPrice;

            var missing = requestedAssets.Where(a => !result.ContainsKey(a.Ticker)).ToList();
            if (missing.Count == 0)
                return result;

            foreach (var asset in missing)
            {
                decimal? fetchedPrice = null;
                var source = string.Empty;
                var currency = "BRL";

                try
                {
                    if (asset.Type == AssetType.Crypto)
                    {
                        var coinId = ResolveCoinGeckoId(asset.Ticker);

                        if (!string.IsNullOrWhiteSpace(coinId))
                        {
                            fetchedPrice = await _coinGeckoClient.GetPriceByIdAsync(
                                coinId,
                                _settings.CoinGeckoVsCurrency,
                                cancellationToken);
                        }

                        if (fetchedPrice is null)
                        {
                            fetchedPrice = await _coinGeckoClient.GetPriceBySymbolAsync(
                                asset.Ticker,
                                _settings.CoinGeckoVsCurrency,
                                cancellationToken);
                        }

                        source = "CoinGecko";
                        currency = _settings.CoinGeckoVsCurrency.ToUpperInvariant();
                    }
                    else
                    {
                        fetchedPrice = await _brapiClient.GetPriceAsync(asset.Ticker, cancellationToken);
                        source = "Brapi";
                        currency = "BRL";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao buscar cotacao para ticker {Ticker}", asset.Ticker);
                }

                if (fetchedPrice is null || fetchedPrice <= 0)
                    continue;

                result[asset.Ticker] = fetchedPrice.Value;

                var quoteEntity = existingQuotes.FirstOrDefault(q => q.Ticker == asset.Ticker);
                if (quoteEntity == null)
                {
                    quoteEntity = new AssetQuote
                    {
                        Id = Guid.NewGuid(),
                        Ticker = asset.Ticker
                    };

                    _assetQuotesRepository.Add(quoteEntity);
                    existingQuotes.Add(quoteEntity);
                }

                quoteEntity.CurrentPrice = fetchedPrice.Value;
                quoteEntity.Source = source;
                quoteEntity.Currency = currency;
                quoteEntity.LastUpdate = now;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }

        private string? ResolveCoinGeckoId(string ticker)
        {
            if (_settings.CoinGeckoTickerToId.TryGetValue(ticker, out var mappedId) && !string.IsNullOrWhiteSpace(mappedId))
                return mappedId;

            return ticker.Trim().ToLowerInvariant();
        }
    }
}
