namespace InvestAPI.DTOs.Portfolio
{
    public class PortfolioSummaryDto
    {
        public int AssetCount { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
