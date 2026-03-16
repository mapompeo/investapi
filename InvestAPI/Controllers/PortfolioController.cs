using InvestAPI.DTOs.Portfolio;
using InvestAPI.Services.Portfolio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ApiControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<PortfolioSummaryDto>> GetSummary()
        {
            var userId = GetCurrentUserIdOrThrow();
            var summary = await _portfolioService.GetSummaryAsync(userId, HttpContext.RequestAborted);
            return Ok(summary);
        }

        [HttpGet("performance")]
        public async Task<ActionResult<IEnumerable<PortfolioPerformanceItemDto>>> GetPerformance()
        {
            var userId = GetCurrentUserIdOrThrow();
            var items = await _portfolioService.GetPerformanceAsync(userId, HttpContext.RequestAborted);
            return Ok(items.OrderByDescending(i => i.CurrentValue));
        }
    }
}
