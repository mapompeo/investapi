using InvestAPI.Data;
using InvestAPI.DTOs.Assets;
using InvestAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AssetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssetsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssetResponseDto>>> GetAll()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var assets = await _context.Assets
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AssetResponseDto
                {
                    Id = a.Id,
                    Ticker = a.Ticker,
                    Type = a.Type,
                    Quantity = a.Quantity,
                    AvgBuyPrice = a.AvgBuyPrice,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync();

            return Ok(assets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AssetResponseDto>> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var asset = await _context.Assets
                .AsNoTracking()
                .Where(a => a.Id == id && a.UserId == userId.Value)
                .Select(a => new AssetResponseDto
                {
                    Id = a.Id,
                    Ticker = a.Ticker,
                    Type = a.Type,
                    Quantity = a.Quantity,
                    AvgBuyPrice = a.AvgBuyPrice,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (asset == null)
                return NotFound();

            return Ok(asset);
        }

        [HttpPost]
        public async Task<ActionResult<AssetResponseDto>> Create([FromBody] CreateAssetDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var ticker = dto.Ticker.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(ticker))
                return BadRequest(new { message = "Ticker é obrigatório." });
            if (dto.Quantity <= 0)
                return BadRequest(new { message = "Quantity deve ser maior que zero." });
            if (dto.AvgBuyPrice <= 0)
                return BadRequest(new { message = "AvgBuyPrice deve ser maior que zero." });

            var alreadyExists = await _context.Assets
                .AnyAsync(a => a.UserId == userId.Value && a.Ticker == ticker);

            if (alreadyExists)
                return Conflict(new { message = "Ativo já existe para o usuário." });

            var now = DateTime.UtcNow;
            var asset = new Assets
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Ticker = ticker,
                Type = dto.Type,
                Quantity = dto.Quantity,
                AvgBuyPrice = dto.AvgBuyPrice,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            var response = new AssetResponseDto
            {
                Id = asset.Id,
                Ticker = asset.Ticker,
                Type = asset.Type,
                Quantity = asset.Quantity,
                AvgBuyPrice = asset.AvgBuyPrice,
                CreatedAt = asset.CreatedAt,
                UpdatedAt = asset.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = asset.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId.Value);

            if (asset == null)
                return NotFound();

            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }
}
