namespace InvestAPI.DTOs.Quotes
{
    public class QuoteRefreshItemDto
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
