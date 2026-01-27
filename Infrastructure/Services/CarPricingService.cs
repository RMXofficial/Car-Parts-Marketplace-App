using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CarPricingService : ICarPricingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CarPricingService> _logger;
    private const decimal VAT_RATE = 0.18m; // 18% VAT for North Macedonia
    private const string EXCHANGE_RATE_API = "https://api.exchangerate-api.com/v4/latest/USD";

    // Cache for exchange rates (simple in-memory cache)
    private static decimal? _cachedExchangeRate;
    private static DateTime? _cacheExpiry;

    public CarPricingService(HttpClient httpClient, ILogger<CarPricingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<decimal> TransformPriceToMKDAsync(decimal originalPrice, string fromCurrency = "USD")
    {
        var result = await GetTransformedPriceWithTaxAsync(originalPrice, fromCurrency);
        return result.PriceInMKD;
    }

    public async Task<PriceTransformationResult> GetTransformedPriceWithTaxAsync(decimal originalPrice, string fromCurrency = "USD")
    {
        try
        {
            // Get exchange rate (USD to MKD)
            decimal exchangeRate = await GetExchangeRateAsync();

            // Convert to MKD
            decimal priceInMKD = originalPrice * exchangeRate;

            // Calculate tax (18% VAT)
            decimal taxAmount = priceInMKD * VAT_RATE;

            // Total with tax
            decimal totalPriceWithTax = priceInMKD + taxAmount;

            return new PriceTransformationResult
            {
                OriginalPrice = originalPrice,
                ExchangeRate = exchangeRate,
                PriceInMKD = priceInMKD,
                TaxAmount = taxAmount,
                TotalPriceWithTax = totalPriceWithTax,
                Currency = "MKD"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming price to MKD");
            // Fallback: use approximate rate if API fails (1 USD ≈ 57 MKD)
            decimal fallbackRate = 57m;
            decimal priceInMKD = originalPrice * fallbackRate;
            decimal taxAmount = priceInMKD * VAT_RATE;

            return new PriceTransformationResult
            {
                OriginalPrice = originalPrice,
                ExchangeRate = fallbackRate,
                PriceInMKD = priceInMKD,
                TaxAmount = taxAmount,
                TotalPriceWithTax = priceInMKD + taxAmount,
                Currency = "MKD"
            };
        }
    }

    private async Task<decimal> GetExchangeRateAsync()
    {
        // Check cache first
        if (_cachedExchangeRate.HasValue && _cacheExpiry.HasValue && DateTime.UtcNow < _cacheExpiry.Value)
        {
            return _cachedExchangeRate.Value;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(EXCHANGE_RATE_API);
            
            if (response?.Rates != null && response.Rates.TryGetValue("MKD", out var rate))
            {
                _cachedExchangeRate = rate;
                _cacheExpiry = DateTime.UtcNow.AddHours(1); // Cache for 1 hour
                return rate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rate from API, using fallback");
        }

        // Fallback rate
        return 57m; // Approximate 1 USD = 57 MKD
    }

    private class ExchangeRateResponse
    {
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
