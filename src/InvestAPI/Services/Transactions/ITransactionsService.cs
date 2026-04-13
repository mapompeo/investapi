using InvestAPI.DTOs.Transactions;

namespace InvestAPI.Services.Transactions;

public interface ITransactionsService
{
    Task<IReadOnlyList<TransactionResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TransactionResponseDto> CreateAsync(Guid userId, CreateTransactionDto dto, CancellationToken cancellationToken = default);
}
