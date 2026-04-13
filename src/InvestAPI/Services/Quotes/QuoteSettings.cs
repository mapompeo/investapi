namespace InvestAPI.Services.Quotes
{
    public class QuoteSettings
    {
        public int CacheMinutes { get; set; } = 5;
        public string BrapiBaseUrl { get; set; } = "https://brapi.dev";
        public string BrapiToken { get; set; } = string.Empty;
        public string CoinGeckoBaseUrl { get; set; } = "https://api.coingecko.com";
        public string CoinGeckoVsCurrency { get; set; } = "usd";
        public Dictionary<string, string> CoinGeckoTickerToId { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BTC"] = "bitcoin",
            ["ETH"] = "ethereum",
            ["SOL"] = "solana",
            ["XRP"] = "ripple",
            ["ADA"] = "cardano",
            ["DOGE"] = "dogecoin"
        };
    }
}
