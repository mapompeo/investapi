using InvestAPI.DTOs.Assets;

namespace InvestAPI.Services.Assets;

public interface IAssetsService
{
    Task<IReadOnlyList<AssetResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AssetResponseDto> GetByIdAsync(Guid userId, Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetResponseDto> CreateAsync(Guid userId, CreateAssetDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid assetId, CancellationToken cancellationToken = default);
}
