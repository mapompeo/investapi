using InvestAPI.DTOs.Assets;
using InvestAPI.Exceptions;
using InvestAPI.Models;
using InvestAPI.Repositories.Assets;
using InvestAPI.Repositories.Common;
using AssetEntity = InvestAPI.Models.Assets;

namespace InvestAPI.Services.Assets;

public class AssetsService : IAssetsService
{
    private readonly IAssetsRepository _assetsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssetsService(IAssetsRepository assetsRepository, IUnitOfWork unitOfWork)
    {
        _assetsRepository = assetsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AssetResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return (await _assetsRepository.GetByUserAsync(userId, asNoTracking: true, cancellationToken))
            .OrderByDescending(a => a.CreatedAt)
            .Select(Map)
            .ToList();
    }

    public async Task<AssetResponseDto> GetByIdAsync(Guid userId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await _assetsRepository.GetByIdForUserAsync(assetId, userId, asNoTracking: true, cancellationToken);

        if (asset is null)
        {
            throw new NotFoundException("Ativo não encontrado.");
        }

        return Map(asset);
    }

    public async Task<AssetResponseDto> CreateAsync(Guid userId, CreateAssetDto dto, CancellationToken cancellationToken = default)
    {
        var ticker = (dto.Ticker ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new BadRequestException("Ticker é obrigatório.");
        }

        if (dto.Quantity <= 0)
        {
            throw new BadRequestException("Quantity deve ser maior que zero.");
        }

        if (dto.AvgBuyPrice <= 0)
        {
            throw new BadRequestException("AvgBuyPrice deve ser maior que zero.");
        }

        var alreadyExists = await _assetsRepository.ExistsByUserAndTickerAsync(userId, ticker, cancellationToken);

        if (alreadyExists)
        {
            throw new ConflictException("Ativo já existe para o usuário.");
        }

        var now = DateTime.UtcNow;
        var asset = new AssetEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Ticker = ticker,
            Type = dto.Type,
            Quantity = dto.Quantity,
            AvgBuyPrice = dto.AvgBuyPrice,
            CreatedAt = now,
            UpdatedAt = now
        };

        _assetsRepository.Add(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(asset);
    }

    public async Task DeleteAsync(Guid userId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await _assetsRepository.GetByIdForUserAsync(assetId, userId, asNoTracking: false, cancellationToken);

        if (asset is null)
        {
            throw new NotFoundException("Ativo não encontrado.");
        }

        _assetsRepository.Remove(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static AssetResponseDto Map(AssetEntity asset)
    {
        return new AssetResponseDto
        {
            Id = asset.Id,
            Ticker = asset.Ticker,
            Type = asset.Type,
            Quantity = asset.Quantity,
            AvgBuyPrice = asset.AvgBuyPrice,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };
    }
}
