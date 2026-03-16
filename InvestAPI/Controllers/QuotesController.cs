using InvestAPI.Data;
using InvestAPI.DTOs.Quotes;
using InvestAPI.Services.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IQuoteService _quoteService;

        public QuotesController(AppDbContext context, IQuoteService quoteService)
        {
            _context = context;
            _quoteService = quoteService;
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<QuoteRefreshResponseDto>> RefreshAll()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var assets = await _context.Assets
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value)
                .Select(a => new AssetQuoteRequest(a.Ticker, a.Type))
                .ToListAsync(HttpContext.RequestAborted);

            if (assets.Count == 0)
            {
                return Ok(new QuoteRefreshResponseDto
                {
                    RequestedCount = 0,
                    RefreshedCount = 0,
                    RefreshedAt = DateTime.UtcNow,
                    Quotes = Array.Empty<QuoteRefreshItemDto>()
                });
            }

            var prices = await _quoteService.GetPricesAsync(assets, forceRefresh: true, cancellationToken: HttpContext.RequestAborted);
            var ordered = prices
                .OrderBy(p => p.Key)
                .Select(p => new QuoteRefreshItemDto { Ticker = p.Key, Price = p.Value })
                .ToList();

            return Ok(new QuoteRefreshResponseDto
            {
                RequestedCount = assets.Count,
                RefreshedCount = ordered.Count,
                RefreshedAt = DateTime.UtcNow,
                Quotes = ordered
            });
        }

        [HttpPost("refresh/{ticker}")]
        public async Task<ActionResult<QuoteRefreshResponseDto>> RefreshByTicker(string ticker)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var normalizedTicker = ticker.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedTicker))
                return BadRequest(new { message = "Ticker é obrigatório." });

            var asset = await _context.Assets
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value && a.Ticker == normalizedTicker)
                .Select(a => new AssetQuoteRequest(a.Ticker, a.Type))
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            if (asset == null)
                return NotFound(new { message = "Ativo não encontrado para o usuário." });

            var prices = await _quoteService.GetPricesAsync(new[] { asset }, forceRefresh: true, cancellationToken: HttpContext.RequestAborted);
            var ordered = prices
                .OrderBy(p => p.Key)
                .Select(p => new QuoteRefreshItemDto { Ticker = p.Key, Price = p.Value })
                .ToList();

            return Ok(new QuoteRefreshResponseDto
            {
                RequestedCount = 1,
                RefreshedCount = ordered.Count,
                RefreshedAt = DateTime.UtcNow,
                Quotes = ordered
            });
        }

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }
}
