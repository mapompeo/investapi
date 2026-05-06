using InvestAPI.DTOs.Dashboard;

namespace InvestAPI.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
