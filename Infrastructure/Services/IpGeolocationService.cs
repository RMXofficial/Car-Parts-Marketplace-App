using System.Net.Http.Json;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class IpGeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IpGeolocationService> _logger;
    private const string BaseUrl = "https://api.ipgeolocation.io/ipgeo"; // v1 API, not v2

    public IpGeolocationService(HttpClient httpClient, IConfiguration configuration, ILogger<IpGeolocationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetCountryCodeAsync(string ipAddress)
    {
        var data = await GetGeolocationDataAsync(ipAddress);
        // Try both country_code2 and country_code
        return data?.CountryCode2 ?? data?.CountryCode ?? "US"; // Default to US (English) if API fails or unknown
    }

    public async Task<string> GetCurrencyCodeAsync(string ipAddress)
    {
        var data = await GetGeolocationDataAsync(ipAddress);
        return data?.Currency?.Code ?? "MKD"; // Default to MKD if unknown
    }

    private async Task<IpGeolocationResponse?> GetGeolocationDataAsync(string ipAddress)
    {
        try
        {
            var apiKey = _configuration["GeolocationApi:ApiKey"] ?? "c84a2460750b4ba6b91ece83cce60d3f";
            
            // If checking localhost, use an empty IP to let the API detect the caller's IP
            // or use a specific IP for testing. 
            // The API documentation says: "When this endpoint is queried without an IP address, it returns the geolocation information of the device/client which is calling it."
            // But since this runs on the server, we might want to pass the user's IP.
            // For now, let's assume we pass the IP. 
            
            string url;
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
            {
                // Localhost - The API call from the server will return the Server's location.
                // In development, this is fine.
                 url = $"{BaseUrl}?apiKey={apiKey}";
            }
            else
            {
                 url = $"{BaseUrl}?apiKey={apiKey}&ip={ipAddress}";
            }

            _logger.LogInformation("Calling geolocation API: {Url}", url.Replace(apiKey, "***"));

            var response = await _httpClient.GetFromJsonAsync<IpGeolocationResponse>(url);

            _logger.LogInformation("Geolocation API response for IP {IP}: CountryCode={CountryCode}, CountryCode2={CountryCode2}, Currency={Currency}",
                ipAddress,
                response?.CountryCode ?? "(null)",
                response?.CountryCode2 ?? "(null)",
                response?.Currency?.Code ?? "(null)");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching geolocation data");
            return null;
        }
    }

    private class IpGeolocationResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("country_code2")]
        public string? CountryCode2 { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("currency")]
        public CurrencyInfo? Currency { get; set; }
    }

    private class CurrencyInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
