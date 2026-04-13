namespace InvestAPI.DTOs.Dashboard
{
    public class DashboardResponseDto
    {
        public int AssetCount { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
        public string? BestPerformerTicker { get; set; }
        public decimal? BestPerformerPercentage { get; set; }
        public string? WorstPerformerTicker { get; set; }
        public decimal? WorstPerformerPercentage { get; set; }
        public DateTime CalculatedAt { get; set; }
        public IReadOnlyList<DashboardAllocationDto> Allocations { get; set; } = Array.Empty<DashboardAllocationDto>();
    }
}
