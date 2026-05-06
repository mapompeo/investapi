namespace InvestAPI.Models
{
    public class Transactions
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Assets Assets { get; set; } = null!;
    }

    public enum TransactionType
    {
        Buy,
        Sell
    }
}