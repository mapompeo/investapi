using InvestAPI.DTOs.Assets;
using InvestAPI.Services.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AssetsController : ApiControllerBase
    {
        private readonly IAssetsService _assetsService;

        public AssetsController(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssetResponseDto>>> GetAll()
        {
            var userId = GetCurrentUserIdOrThrow();
            var assets = await _assetsService.GetAllAsync(userId, HttpContext.RequestAborted);
            return Ok(assets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AssetResponseDto>> GetById(Guid id)
        {
            var userId = GetCurrentUserIdOrThrow();
            var asset = await _assetsService.GetByIdAsync(userId, id, HttpContext.RequestAborted);
            return Ok(asset);
        }

        [HttpPost]
        public async Task<ActionResult<AssetResponseDto>> Create([FromBody] CreateAssetDto dto)
        {
            var userId = GetCurrentUserIdOrThrow();
            var response = await _assetsService.CreateAsync(userId, dto, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserIdOrThrow();
            await _assetsService.DeleteAsync(userId, id, HttpContext.RequestAborted);

            return NoContent();
        }
    }
}
