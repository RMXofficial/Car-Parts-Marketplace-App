using Application.Commands.Listings;
using Application.DTOs;
using Application.Queries.Listings;
using Application.Queries.Pricing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ViewModels;
using System.Security.Claims;
using Domain.Interfaces;
using Infrastructure.Services;

namespace Web.Controllers;

public class ListingsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<ListingsController> _logger;
    private readonly IGeolocationService _geolocationService;
    private readonly ICurrencyService _currencyService;

    public ListingsController(
        IMediator mediator, 
        ILogger<ListingsController> logger,
        IGeolocationService geolocationService,
        ICurrencyService currencyService)
    {
        _mediator = mediator;
        _logger = logger;
        _geolocationService = geolocationService;
        _currencyService = currencyService;
    }

    // GET: Listings
    [Authorize]
    public async Task<IActionResult> Index(string? category, string? listingType)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var query = new GetAllListingsQuery
        {
            ActiveOnly = true,
            Category = category,
            ListingType = listingType,
            UserId = userId
        };

        var listings = await _mediator.Send(query);

        // Detect user's currency
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userCurrency = await _geolocationService.GetCurrencyCodeAsync(ipAddress ?? "");

        // Convert prices for each listing
        var listingViewModels = new List<ListingIndexItemViewModel>();
        foreach (var listing in listings)
        {
            var convertedPrice = await _currencyService.ConvertAsync(listing.Price, listing.Currency, userCurrency);

            var priceTransformation = new PriceTransformationDto
            {
                OriginalPrice = listing.Price,
                ExchangeRate = convertedPrice / (listing.Price == 0 ? 1 : listing.Price),
                PriceInMKD = convertedPrice,
                TaxAmount = 0,
                TotalPriceWithTax = convertedPrice,
                Currency = userCurrency
            };

            listingViewModels.Add(new ListingIndexItemViewModel
            {
                Listing = listing,
                PriceTransformation = priceTransformation
            });
        }

        return View(listingViewModels);
    }

    // GET: Listings/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var query = new GetListingByIdQuery { Id = id };
        var listing = await _mediator.Send(query);

        if (listing == null)
        {
            return NotFound();
        }

        // Detect user's currency
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userCurrency = await _geolocationService.GetCurrencyCodeAsync(ipAddress ?? "");
        
        // Convert price if different
        // Reusing PriceTransformationResult structure but populating it manually for now
        // leveraging the logic we have in CarPricingService might be better if we want Tax calculation
        // But the user asked for currency conversion specifically.
        
        var convertedPrice = await _currencyService.ConvertAsync(listing.Price, listing.Currency, userCurrency);

        // Get price transformation (Legacy logic kept for reference, but updated to use new service results)
        // We act like the Listing Price is the Original Price
        
        var priceTransformation = new PriceTransformationDto
        {
            OriginalPrice = listing.Price,
            ExchangeRate = convertedPrice / (listing.Price == 0 ? 1 : listing.Price), // Approximate rate
            PriceInMKD = convertedPrice, // This property name is legacy specific to MKD, but we use it as "Converted Price"
            TaxAmount = 0, // Tax logic might need revisit
            TotalPriceWithTax = convertedPrice,
            Currency = userCurrency
        };

        var viewModel = new ListingDetailsViewModel
        {
            Listing = listing,
            PriceTransformation = priceTransformation
        };

        return View(viewModel);
    }

    // GET: Listings/Create
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Create()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userCurrency = await _geolocationService.GetCurrencyCodeAsync(ipAddress ?? "");
        
        var model = new CreateListingDto
        {
             Currency = userCurrency
        };
        
        return View(model);
    }

    // POST: Listings/Create
    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateListingDto listingDto)
    {
        if (ModelState.IsValid)
        {
            // Get userId from authentication
            listingDto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            var command = new CreateListingCommand { Listing = listingDto };
            var result = await _mediator.Send(command);
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }

        return View(listingDto);
    }

    // GET: Listings/Edit/5
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var query = new GetListingByIdQuery { Id = id };
        var listing = await _mediator.Send(query);

        if (listing == null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId) || (listing.UserId != currentUserId && !User.IsInRole("Admin")))
            return Forbid();

        ViewBag.ListingId = id;
        var editDto = new CreateListingDto
        {
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            CategoryId = listing.CategoryId,
            ListingType = listing.ListingType,
            Make = listing.Make,
            Model = listing.Model,
            Year = listing.Year,
            Condition = listing.Condition,
            ImageUrl = listing.ImageUrl,
            UserId = listing.UserId
        };

        return View(editDto);
    }

    // POST: Listings/Edit/5
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateListingDto listingDto)
    {
        // Validation is handled by ModelState

        if (ModelState.IsValid)
        {
            try
            {
                var existingListing = await _mediator.Send(new GetListingByIdQuery { Id = id });
                if (existingListing == null)
                {
                    return NotFound();
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId) || (existingListing.UserId != currentUserId && !User.IsInRole("Admin")))
                {
                    return Forbid();
                }

                var command = new UpdateListingCommand
                {
                    Id = id,
                    Listing = listingDto
                };
                var result = await _mediator.Send(command);
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating listing {ListingId}", id);
                ModelState.AddModelError("", "An error occurred while updating the listing.");
            }
        }

        ViewBag.ListingId = id;
        return View(listingDto);
    }

    // GET: Listings/Delete/5
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var query = new GetListingByIdQuery { Id = id };
        var listing = await _mediator.Send(query);

        if (listing == null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId) || (listing.UserId != currentUserId && !User.IsInRole("Admin")))
        {
            return Forbid();
        }

        // Detect user's currency
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userCurrency = await _geolocationService.GetCurrencyCodeAsync(ipAddress ?? "");

        // Convert price if different
        var convertedPrice = await _currencyService.ConvertAsync(listing.Price, listing.Currency, userCurrency);

        var priceTransformation = new PriceTransformationDto
        {
            OriginalPrice = listing.Price,
            ExchangeRate = convertedPrice / (listing.Price == 0 ? 1 : listing.Price),
            PriceInMKD = convertedPrice,
            TaxAmount = 0,
            TotalPriceWithTax = convertedPrice,
            Currency = userCurrency
        };

        var viewModel = new ListingDetailsViewModel
        {
            Listing = listing,
            PriceTransformation = priceTransformation
        };

        return View(viewModel);
    }

    // POST: Listings/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var listing = await _mediator.Send(new GetListingByIdQuery { Id = id });
        if (listing == null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId) || (listing.UserId != currentUserId && !User.IsInRole("Admin")))
        {
            return Forbid();
        }

        try
        {
            var command = new DeleteListingCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
