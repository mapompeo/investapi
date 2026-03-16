using InvestAPI.DTOs.Transactions;
using InvestAPI.Services.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ApiControllerBase
    {
        private readonly ITransactionsService _transactionsService;

        public TransactionsController(ITransactionsService transactionsService)
        {
            _transactionsService = transactionsService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetAll()
        {
            var userId = GetCurrentUserIdOrThrow();
            var transactions = await _transactionsService.GetAllAsync(userId, HttpContext.RequestAborted);
            return Ok(transactions);
        }

        [HttpPost]
        public async Task<ActionResult<TransactionResponseDto>> Create([FromBody] CreateTransactionDto dto)
        {
            var userId = GetCurrentUserIdOrThrow();
            var response = await _transactionsService.CreateAsync(userId, dto, HttpContext.RequestAborted);
            return Created(string.Empty, response);
        }
    }
}
