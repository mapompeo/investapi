using InvestAPI.DTOs.Quotes;
using InvestAPI.Services.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class QuotesController : ApiControllerBase
    {
        private readonly IQuotesManagementService _quotesManagementService;

        public QuotesController(IQuotesManagementService quotesManagementService)
        {
            _quotesManagementService = quotesManagementService;
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<QuoteRefreshResponseDto>> RefreshAll()
        {
            var userId = GetCurrentUserIdOrThrow();
            var response = await _quotesManagementService.RefreshAllAsync(userId, HttpContext.RequestAborted);
            return Ok(response);
        }

        [HttpPost("refresh/{ticker}")]
        public async Task<ActionResult<QuoteRefreshResponseDto>> RefreshByTicker(string ticker)
        {
            var userId = GetCurrentUserIdOrThrow();
            var response = await _quotesManagementService.RefreshByTickerAsync(userId, ticker, HttpContext.RequestAborted);
            return Ok(response);
        }
    }
}
