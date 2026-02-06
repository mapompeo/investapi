namespace InvestAPI.Models
{
    public class Assets
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public AssetType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvgBuyPrice { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Users User { get; set; } = null!;
        public ICollection<Transactions> Transactions { get; set; } = new List<Transaction>();

    }
    public enum AssetType
    {
        Stock,
        Crypto,
        FII
    }
}
