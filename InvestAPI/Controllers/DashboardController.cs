using InvestAPI.Data;
using InvestAPI.DTOs.Dashboard;
using InvestAPI.Services.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IQuoteService _quoteService;

        public DashboardController(AppDbContext context, IQuoteService quoteService)
        {
            _context = context;
            _quoteService = quoteService;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardResponseDto>> Get()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var assets = await _context.Assets
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value)
                .ToListAsync(HttpContext.RequestAborted);

            var assetIds = assets.Select(a => a.Id).ToList();
            var totalTransactions = assetIds.Count == 0
                ? 0
                : await _context.Transactions.AsNoTracking().CountAsync(t => assetIds.Contains(t.AssetId), HttpContext.RequestAborted);

            if (assets.Count == 0)
            {
                return Ok(new DashboardResponseDto
                {
                    AssetCount = 0,
                    TotalTransactions = totalTransactions,
                    TotalInvested = 0,
                    CurrentValue = 0,
                    ProfitLoss = 0,
                    ProfitLossPercentage = 0,
                    CalculatedAt = DateTime.UtcNow,
                    Allocations = Array.Empty<DashboardAllocationDto>()
                });
            }

            var quoteRequests = assets
                .Select(a => new AssetQuoteRequest(a.Ticker, a.Type))
                .ToList();

            var quotes = await _quoteService.GetPricesAsync(quoteRequests, cancellationToken: HttpContext.RequestAborted);

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

            return Ok(new DashboardResponseDto
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
            });
        }

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }
}
