using InvestAPI.Models;

namespace InvestAPI.DTOs.Transactions
{
    public class TransactionResponseDto
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string AssetTicker { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
