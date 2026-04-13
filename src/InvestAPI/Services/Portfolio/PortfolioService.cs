using InvestAPI.DTOs.Portfolio;
using InvestAPI.Repositories.Assets;
using InvestAPI.Services.Quotes;

namespace InvestAPI.Services.Portfolio;

public class PortfolioService : IPortfolioService
{
    private readonly IAssetsRepository _assetsRepository;
    private readonly IQuoteService _quoteService;

    public PortfolioService(IAssetsRepository assetsRepository, IQuoteService quoteService)
    {
        _assetsRepository = assetsRepository;
        _quoteService = quoteService;
    }

    public async Task<PortfolioSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await BuildPerformanceItems(userId, cancellationToken);

        var totalInvested = items.Sum(i => i.TotalInvested);
        var currentValue = items.Sum(i => i.CurrentValue);
        var profitLoss = currentValue - totalInvested;
        var profitLossPercentage = totalInvested == 0 ? 0 : (profitLoss / totalInvested) * 100;

        return new PortfolioSummaryDto
        {
            AssetCount = items.Count,
            TotalInvested = totalInvested,
            CurrentValue = currentValue,
            ProfitLoss = profitLoss,
            ProfitLossPercentage = profitLossPercentage,
            CalculatedAt = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<PortfolioPerformanceItemDto>> GetPerformanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await BuildPerformanceItems(userId, cancellationToken);
        return items.OrderByDescending(i => i.CurrentValue).ToList();
    }

    private async Task<List<PortfolioPerformanceItemDto>> BuildPerformanceItems(Guid userId, CancellationToken cancellationToken)
    {
        var assets = await _assetsRepository.GetByUserAsync(userId, asNoTracking: true, cancellationToken);

        if (assets.Count == 0)
        {
            return new List<PortfolioPerformanceItemDto>();
        }

        var quoteRequests = assets
            .Select(a => new AssetQuoteRequest(a.Ticker, a.Type))
            .ToList();

        var quotes = await _quoteService.GetPricesAsync(quoteRequests, cancellationToken: cancellationToken);

        var items = new List<PortfolioPerformanceItemDto>(assets.Count);

        foreach (var asset in assets)
        {
            var currentPrice = quotes.TryGetValue(asset.Ticker, out var quotePrice)
                ? quotePrice
                : asset.AvgBuyPrice;

            var totalInvested = asset.Quantity * asset.AvgBuyPrice;
            var currentValue = asset.Quantity * currentPrice;
            var profitLoss = currentValue - totalInvested;
            var profitLossPercentage = totalInvested == 0 ? 0 : (profitLoss / totalInvested) * 100;

            items.Add(new PortfolioPerformanceItemDto
            {
                AssetId = asset.Id,
                Ticker = asset.Ticker,
                Type = asset.Type,
                Quantity = asset.Quantity,
                AvgBuyPrice = asset.AvgBuyPrice,
                CurrentPrice = currentPrice,
                TotalInvested = totalInvested,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss,
                ProfitLossPercentage = profitLossPercentage
            });
        }

        return items;
    }
}
