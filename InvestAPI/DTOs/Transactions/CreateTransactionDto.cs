using InvestAPI.Models;

namespace InvestAPI.DTOs.Transactions
{
    public class CreateTransactionDto
    {
        public Guid AssetId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime? Date { get; set; }
        public string? Notes { get; set; }
    }
}
