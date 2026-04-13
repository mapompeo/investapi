namespace InvestAPI.DTOs.Quotes
{
    public class QuoteRefreshResponseDto
    {
        public int RequestedCount { get; set; }
        public int RefreshedCount { get; set; }
        public DateTime RefreshedAt { get; set; }
        public IReadOnlyList<QuoteRefreshItemDto> Quotes { get; set; } = Array.Empty<QuoteRefreshItemDto>();
    }
}
