using System.Globalization;
using Domain.Interfaces;
using Microsoft.AspNetCore.Localization;

namespace Web.Middleware;

public class GeolocationLocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GeolocationLocalizationMiddleware> _logger;

    public GeolocationLocalizationMiddleware(RequestDelegate next, ILogger<GeolocationLocalizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IGeolocationService geoService)
    {
        var cultureCookie = context.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];

        _logger.LogInformation("Geolocation Middleware - Cookie: {Cookie}", cultureCookie ?? "(none)");

        if (string.IsNullOrEmpty(cultureCookie))
        {
            try
            {
                var ip = context.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation("Geolocation Middleware - IP: {IP}", ip ?? "(null)");

                var countryCode = await geoService.GetCountryCodeAsync(ip ?? "");
                _logger.LogInformation("Geolocation Middleware - Country: {Country}", countryCode);

                // Macedonia -> mk-MK (Macedonian site), otherwise -> en-US (English)
                var culture = string.Equals(countryCode, "MK", StringComparison.OrdinalIgnoreCase) ? "mk-MK" : "en-US";
                _logger.LogInformation("Geolocation Middleware - Setting culture: {Culture}", culture);

                var requestCulture = new RequestCulture(culture);

                // Apply to current request so this page load uses it (not only the next one)
                var feature = new RequestCultureFeature(requestCulture, null);
                context.Features.Set<IRequestCultureFeature>(feature);

                var cultureInfo = new CultureInfo(culture);
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;

                context.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(requestCulture),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), SameSite = SameSiteMode.Lax }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geolocation Middleware - Error detecting location");
            }
        }

        await _next(context);
    }
}
