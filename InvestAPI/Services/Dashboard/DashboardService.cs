using InvestAPI.DTOs.Dashboard;
using InvestAPI.Repositories.Assets;
using InvestAPI.Repositories.Transactions;
using InvestAPI.Services.Quotes;

namespace InvestAPI.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IAssetsRepository _assetsRepository;
    private readonly ITransactionsRepository _transactionsRepository;
    private readonly IQuoteService _quoteService;

    public DashboardService(IAssetsRepository assetsRepository, ITransactionsRepository transactionsRepository, IQuoteService quoteService)
    {
        _assetsRepository = assetsRepository;
        _transactionsRepository = transactionsRepository;
        _quoteService = quoteService;
    }

    public async Task<DashboardResponseDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var assets = await _assetsRepository.GetByUserAsync(userId, asNoTracking: true, cancellationToken);

        var assetIds = assets.Select(a => a.Id).ToList();
        var totalTransactions = await _transactionsRepository.CountByAssetIdsAsync(assetIds, cancellationToken);

        if (assets.Count == 0)
        {
            return new DashboardResponseDto
            {
                AssetCount = 0,
                TotalTransactions = totalTransactions,
                TotalInvested = 0,
                CurrentValue = 0,
                ProfitLoss = 0,
                ProfitLossPercentage = 0,
                CalculatedAt = DateTime.UtcNow,
                Allocations = Array.Empty<DashboardAllocationDto>()
            };
        }

        var quoteRequests = assets
            .Select(a => new AssetQuoteRequest(a.Ticker, a.Type))
            .ToList();

        var quotes = await _quoteService.GetPricesAsync(quoteRequests, cancellationToken: cancellationToken);

        var rows = assets.Select(asset =>
        {
            var currentPrice = quotes.TryGetValue(asset.Ticker, out var quotePrice)
                ? quotePrice
                : asset.AvgBuyPrice;

            var totalInvested = asset.Quantity * asset.AvgBuyPrice;
            var currentValue = asset.Quantity * currentPrice;
            var profitLoss = currentValue - totalInvested;
            var profitLossPercentage = totalInvested == 0 ? 0 : (profitLoss / totalInvested) * 100;

            return new
            {
                asset.Ticker,
                CurrentValue = currentValue,
                TotalInvested = totalInvested,
                ProfitLoss = profitLoss,
                ProfitLossPercentage = profitLossPercentage
            };
        }).ToList();

        var totalInvestedAll = rows.Sum(r => r.TotalInvested);
        var currentValueAll = rows.Sum(r => r.CurrentValue);
        var profitLossAll = currentValueAll - totalInvestedAll;
        var profitLossPercentageAll = totalInvestedAll == 0 ? 0 : (profitLossAll / totalInvestedAll) * 100;

        var best = rows.OrderByDescending(r => r.ProfitLossPercentage).FirstOrDefault();
        var worst = rows.OrderBy(r => r.ProfitLossPercentage).FirstOrDefault();

        var allocations = rows
            .OrderByDescending(r => r.CurrentValue)
            .Select(r => new DashboardAllocationDto
            {
                Ticker = r.Ticker,
                CurrentValue = r.CurrentValue,
                AllocationPercentage = currentValueAll == 0 ? 0 : (r.CurrentValue / currentValueAll) * 100
            })
            .ToList();

        return new DashboardResponseDto
        {
            AssetCount = assets.Count,
            TotalTransactions = totalTransactions,
            TotalInvested = totalInvestedAll,
            CurrentValue = currentValueAll,
            ProfitLoss = profitLossAll,
            ProfitLossPercentage = profitLossPercentageAll,
            BestPerformerTicker = best?.Ticker,
            BestPerformerPercentage = best?.ProfitLossPercentage,
            WorstPerformerTicker = worst?.Ticker,
            WorstPerformerPercentage = worst?.ProfitLossPercentage,
            CalculatedAt = DateTime.UtcNow,
            Allocations = allocations
        };
    }
}
