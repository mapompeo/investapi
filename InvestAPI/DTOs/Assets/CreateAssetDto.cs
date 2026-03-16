using InvestAPI.Models;

namespace InvestAPI.DTOs.Assets
{
    public class CreateAssetDto
    {
        public string Ticker { get; set; } = string.Empty;
        public AssetType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvgBuyPrice { get; set; }
    }
}
