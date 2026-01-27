using Application.Commands.Orders;
using Application.DTOs;
using Application.Queries.Orders;
using Application.Queries.Listings;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.ViewModels;
using Web.Models;
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace Web.Controllers;

public class OrdersController : Controller
{
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly ICurrencyService _currencyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrdersController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        IMediator mediator,
        IEmailService emailService,
        ICurrencyService currencyService,
        IConfiguration configuration,
        ILogger<OrdersController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _mediator = mediator;
        _emailService = emailService;
        _currencyService = currencyService;
        _configuration = configuration;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: Orders
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Index()
    {
        // Get userId from authentication
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var query = new GetUserOrdersQuery { UserId = userId };
        var orders = await _mediator.Send(query);

        // Check if user is on Macedonian site and convert prices to MKD
        var isMacedonianSite = CultureInfo.CurrentUICulture.Name.StartsWith("mk", StringComparison.OrdinalIgnoreCase);
        ViewBag.IsMacedonianSite = isMacedonianSite;

        if (isMacedonianSite)
        {
            var convertedPrices = new Dictionary<int, decimal>();
            foreach (var order in orders)
            {
                // Assuming orders are in USD
                var mkdPrice = await _currencyService.ConvertAsync(order.TotalAmount, "USD", "MKD");
                convertedPrices[order.Id] = mkdPrice;
            }
            ViewBag.ConvertedPrices = convertedPrices;
        }

        return View(orders);
    }

    // GET: Orders/Create
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Create(int listingId)
    {
        var listingQuery = new GetListingByIdQuery { Id = listingId };
        var listing = await _mediator.Send(listingQuery);

        if (listing == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        var viewModel = new CreateOrderViewModel
        {
            ListingId = listingId,
            Listing = listing,
            ShippingAddress = user?.Address ?? "",
            ShippingCity = user?.City,
            ShippingPostalCode = user?.PostalCode,
            ShippingCountry = user?.Country ?? "North Macedonia"
        };

        // Check if user is on Macedonian site and convert price to MKD
        var isMacedonianSite = CultureInfo.CurrentUICulture.Name.StartsWith("mk", StringComparison.OrdinalIgnoreCase);
        if (isMacedonianSite && !string.Equals(listing.Currency, "MKD", StringComparison.OrdinalIgnoreCase))
        {
            viewModel.ConvertedPrice = await _currencyService.ConvertAsync(listing.Price, listing.Currency, "MKD");
            viewModel.ConvertedCurrency = "MKD";
        }

        return View(viewModel);
    }

    // POST: Orders/Create
    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            var orderDto = new CreateOrderDto
            {
                UserId = userId,
                ShippingAddress = viewModel.ShippingAddress,
                ShippingCity = viewModel.ShippingCity,
                ShippingPostalCode = viewModel.ShippingPostalCode,
                ShippingCountry = viewModel.ShippingCountry ?? "North Macedonia",
                Items = new List<OrderItemCreateDto>
                {
                    new OrderItemCreateDto { ListingId = viewModel.ListingId, Quantity = viewModel.Quantity }
                }
            };

            var command = new CreateOrderCommand { Order = orderDto };
            var order = await _mediator.Send(command);

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // Reload listing if validation fails
        var listingQuery = new GetListingByIdQuery { Id = viewModel.ListingId };
        viewModel.Listing = await _mediator.Send(listingQuery);

        return View(viewModel);
    }


    // GET: Orders/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var query = new GetOrderByIdQuery { Id = id };
        var order = await _mediator.Send(query);

        if (order == null)
        {
            return NotFound();
        }

        // Check if user is on Macedonian site and convert prices to MKD
        var isMacedonianSite = CultureInfo.CurrentUICulture.Name.StartsWith("mk", StringComparison.OrdinalIgnoreCase);
        ViewBag.IsMacedonianSite = isMacedonianSite;

        if (isMacedonianSite)
        {
            // Convert total amount
            ViewBag.ConvertedTotal = await _currencyService.ConvertAsync(order.TotalAmount, "USD", "MKD");

            // Convert order items prices
            var convertedItems = new Dictionary<int, (decimal UnitPrice, decimal Subtotal)>();
            foreach (var item in order.OrderItems)
            {
                var unitPrice = await _currencyService.ConvertAsync(item.UnitPrice, "USD", "MKD");
                var subtotal = await _currencyService.ConvertAsync(item.Subtotal, "USD", "MKD");
                convertedItems[item.Id] = (unitPrice, subtotal);
            }
            ViewBag.ConvertedItems = convertedItems;
        }

        return View(order);
    }
}
