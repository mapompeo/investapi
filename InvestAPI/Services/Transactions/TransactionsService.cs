using InvestAPI.DTOs.Transactions;
using InvestAPI.Exceptions;
using InvestAPI.Models;
using InvestAPI.Repositories.Assets;
using InvestAPI.Repositories.Common;
using InvestAPI.Repositories.Transactions;
using TransactionEntity = InvestAPI.Models.Transactions;

namespace InvestAPI.Services.Transactions;

public class TransactionsService : ITransactionsService
{
    private readonly IAssetsRepository _assetsRepository;
    private readonly ITransactionsRepository _transactionsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionsService(IAssetsRepository assetsRepository, ITransactionsRepository transactionsRepository, IUnitOfWork unitOfWork)
    {
        _assetsRepository = assetsRepository;
        _transactionsRepository = transactionsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TransactionResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return (await _transactionsRepository.GetByUserAsync(userId, cancellationToken))
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
            .ToList();
    }

    public async Task<TransactionResponseDto> CreateAsync(Guid userId, CreateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0)
        {
            throw new BadRequestException("Quantity deve ser maior que zero.");
        }

        if (dto.Price <= 0)
        {
            throw new BadRequestException("Price deve ser maior que zero.");
        }

        var asset = await _assetsRepository.GetByIdForUserAsync(dto.AssetId, userId, asNoTracking: false, cancellationToken);

        if (asset is null)
        {
            throw new NotFoundException("Ativo não encontrado para o usuário.");
        }

        if (dto.Type == TransactionType.Sell && asset.Quantity < dto.Quantity)
        {
            throw new BadRequestException("Quantidade insuficiente para venda.");
        }

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
            {
                asset.AvgBuyPrice = 0;
            }
        }

        asset.UpdatedAt = DateTime.UtcNow;

        var transaction = new TransactionEntity
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

        _transactionsRepository.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResponseDto
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
    }
}
