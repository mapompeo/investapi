using InvestAPI.DTOs.Dashboard;
using InvestAPI.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DashboardController : ApiControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardResponseDto>> Get()
        {
            var userId = GetCurrentUserIdOrThrow();
            var dashboard = await _dashboardService.GetAsync(userId, HttpContext.RequestAborted);
            return Ok(dashboard);
        }
    }
}
