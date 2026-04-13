using InvestAPI.DTOs.Quotes;

namespace InvestAPI.Services.Quotes;

public interface IQuotesManagementService
{
    Task<QuoteRefreshResponseDto> RefreshAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<QuoteRefreshResponseDto> RefreshByTickerAsync(Guid userId, string ticker, CancellationToken cancellationToken = default);
}
