using InvestAPI.Data;
using InvestAPI.DTOs.Transactions;
using InvestAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetAll()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Assets.UserId == userId.Value)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => new TransactionResponseDto
                {
                    Id = t.Id,
                    AssetId = t.AssetId,
                    AssetTicker = t.Assets.Ticker,
                    Type = t.Type,
                    Quantity = t.Quantity,
                    Price = t.Price,
                    TotalValue = t.TotalValue,
                    Date = t.Date,
                    Notes = t.Notes,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpPost]
        public async Task<ActionResult<TransactionResponseDto>> Create([FromBody] CreateTransactionDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            if (dto.Quantity <= 0)
                return BadRequest(new { message = "Quantity deve ser maior que zero." });
            if (dto.Price <= 0)
                return BadRequest(new { message = "Price deve ser maior que zero." });

            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Id == dto.AssetId && a.UserId == userId.Value);

            if (asset == null)
                return NotFound(new { message = "Ativo não encontrado para o usuário." });

            if (dto.Type == TransactionType.Sell && asset.Quantity < dto.Quantity)
                return BadRequest(new { message = "Quantidade insuficiente para venda." });

            if (dto.Type == TransactionType.Buy)
            {
                var currentTotalInvested = asset.Quantity * asset.AvgBuyPrice;
                var newTotalInvested = dto.Quantity * dto.Price;
                var newQuantity = asset.Quantity + dto.Quantity;

                asset.AvgBuyPrice = (currentTotalInvested + newTotalInvested) / newQuantity;
                asset.Quantity = newQuantity;
            }
            else
            {
                asset.Quantity -= dto.Quantity;

                if (asset.Quantity == 0)
                    asset.AvgBuyPrice = 0;
            }

            asset.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transactions
            {
                Id = Guid.NewGuid(),
                AssetId = asset.Id,
                Type = dto.Type,
                Quantity = dto.Quantity,
                Price = dto.Price,
                TotalValue = dto.Quantity * dto.Price,
                Date = dto.Date ?? DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var response = new TransactionResponseDto
            {
                Id = transaction.Id,
                AssetId = transaction.AssetId,
                AssetTicker = asset.Ticker,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                Price = transaction.Price,
                TotalValue = transaction.TotalValue,
                Date = transaction.Date,
                Notes = transaction.Notes,
                CreatedAt = transaction.CreatedAt
            };

            return Created(string.Empty, response);
        }

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }
}
