namespace InvestAPI.Models
{
    public class AssetQuote
    {
        public Guid Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }
}