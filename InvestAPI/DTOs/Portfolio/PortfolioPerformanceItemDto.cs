using InvestAPI.Models;

namespace InvestAPI.DTOs.Portfolio
{
    public class PortfolioPerformanceItemDto
    {
        public Guid AssetId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public AssetType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvgBuyPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
    }
}
