using InvestAPI.Data;
using InvestAPI.DTOs.Portfolio;
using InvestAPI.Services.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IQuoteService _quoteService;

        public PortfolioController(AppDbContext context, IQuoteService quoteService)
        {
            _context = context;
            _quoteService = quoteService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<PortfolioSummaryDto>> GetSummary()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var items = await BuildPerformanceItems(userId.Value, HttpContext.RequestAborted);

            var totalInvested = items.Sum(i => i.TotalInvested);
            var currentValue = items.Sum(i => i.CurrentValue);
            var profitLoss = currentValue - totalInvested;
            var profitLossPercentage = totalInvested == 0 ? 0 : (profitLoss / totalInvested) * 100;

            return Ok(new PortfolioSummaryDto
            {
                AssetCount = items.Count,
                TotalInvested = totalInvested,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss,
                ProfitLossPercentage = profitLossPercentage,
                CalculatedAt = DateTime.UtcNow
            });
        }

        [HttpGet("performance")]
        public async Task<ActionResult<IEnumerable<PortfolioPerformanceItemDto>>> GetPerformance()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var items = await BuildPerformanceItems(userId.Value, HttpContext.RequestAborted);
            return Ok(items.OrderByDescending(i => i.CurrentValue));
        }

        private async Task<List<PortfolioPerformanceItemDto>> BuildPerformanceItems(Guid userId, CancellationToken cancellationToken)
        {
            var assets = await _context.Assets
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync(cancellationToken);

            if (assets.Count == 0)
                return new List<PortfolioPerformanceItemDto>();

            var quoteRequests = assets
                .Select(a => new AssetQuoteRequest(a.Ticker, a.Type))
                .ToList();

            var quotes = await _quoteService.GetPricesAsync(quoteRequests, cancellationToken);

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

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }
}
