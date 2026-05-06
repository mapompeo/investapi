using InvestAPI.DTOs.Quotes;
using InvestAPI.Exceptions;
using InvestAPI.Repositories.Assets;

namespace InvestAPI.Services.Quotes;

public class QuotesManagementService : IQuotesManagementService
{
    private readonly IAssetsRepository _assetsRepository;
    private readonly IQuoteService _quoteService;

    public QuotesManagementService(IAssetsRepository assetsRepository, IQuoteService quoteService)
    {
        _assetsRepository = assetsRepository;
        _quoteService = quoteService;
    }

    public async Task<QuoteRefreshResponseDto> RefreshAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var assets = (await _assetsRepository.GetByUserAsync(userId, asNoTracking: true, cancellationToken))
            .Select(a => new AssetQuoteRequest(a.Ticker, a.Type))
            .ToList();

        if (assets.Count == 0)
        {
            return new QuoteRefreshResponseDto
            {
                RequestedCount = 0,
                RefreshedCount = 0,
                RefreshedAt = DateTime.UtcNow,
                Quotes = Array.Empty<QuoteRefreshItemDto>()
            };
        }

        var prices = await _quoteService.GetPricesAsync(assets, forceRefresh: true, cancellationToken: cancellationToken);
        var ordered = prices
            .OrderBy(p => p.Key)
            .Select(p => new QuoteRefreshItemDto { Ticker = p.Key, Price = p.Value })
            .ToList();

        return new QuoteRefreshResponseDto
        {
            RequestedCount = assets.Count,
            RefreshedCount = ordered.Count,
            RefreshedAt = DateTime.UtcNow,
            Quotes = ordered
        };
    }

    public async Task<QuoteRefreshResponseDto> RefreshByTickerAsync(Guid userId, string ticker, CancellationToken cancellationToken = default)
    {
        var normalizedTicker = ticker.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedTicker))
        {
            throw new BadRequestException("Ticker é obrigatório.");
        }

        var assetEntity = await _assetsRepository.GetByTickerForUserAsync(userId, normalizedTicker, asNoTracking: true, cancellationToken);

        var asset = assetEntity is null ? null : new AssetQuoteRequest(assetEntity.Ticker, assetEntity.Type);

        if (asset is null)
        {
            throw new NotFoundException("Ativo não encontrado para o usuário.");
        }

        var prices = await _quoteService.GetPricesAsync(new[] { asset }, forceRefresh: true, cancellationToken: cancellationToken);
        var ordered = prices
            .OrderBy(p => p.Key)
            .Select(p => new QuoteRefreshItemDto { Ticker = p.Key, Price = p.Value })
            .ToList();

        return new QuoteRefreshResponseDto
        {
            RequestedCount = 1,
            RefreshedCount = ordered.Count,
            RefreshedAt = DateTime.UtcNow,
            Quotes = ordered
        };
    }
}
