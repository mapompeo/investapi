using InvestAPI.Models;

namespace InvestAPI.DTOs.Assets
{
    public class AssetResponseDto
    {
        public Guid Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public AssetType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvgBuyPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
