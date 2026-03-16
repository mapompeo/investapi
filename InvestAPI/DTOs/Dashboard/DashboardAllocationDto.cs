namespace InvestAPI.DTOs.Dashboard
{
    public class DashboardAllocationDto
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal AllocationPercentage { get; set; }
    }
}
