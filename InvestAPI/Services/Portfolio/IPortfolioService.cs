using InvestAPI.DTOs.Portfolio;

namespace InvestAPI.Services.Portfolio;

public interface IPortfolioService
{
    Task<PortfolioSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortfolioPerformanceItemDto>> GetPerformanceAsync(Guid userId, CancellationToken cancellationToken = default);
}
