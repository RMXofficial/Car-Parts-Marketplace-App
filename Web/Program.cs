using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// Add Infrastructure services (Database, Repositories, External Services)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Add Application services (MediatR, AutoMapper)
builder.Services.AddApplication();

var app = builder.Build();

// Seed database on startup and enable foreign keys for SQLite
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
        
        // Enable foreign keys for SQLite before any operations
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }
        
        await Infrastructure.Data.DatabaseSeeder.SeedAsync(context, services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Localization
var supportedCultures = new[] { "en-US", "mk-MK" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
app.UseMiddleware<Web.Middleware.GeolocationLocalizationMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Handle migration command
if (args.Contains("--migrate-admin"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infrastructure.Data.ApplicationUser>>();
        
        var email = "admin@carparts.com";
        var password = "Admin@123456";
        
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            // Delete existing user to recreate
            await userManager.DeleteAsync(user);
        }
        
        var newUser = new Infrastructure.Data.ApplicationUser
        {
            UserName = "admin",
            Email = email,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User"
        };
        
        var result = await userManager.CreateAsync(newUser, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newUser, "Admin");
            Console.WriteLine($"✓ Admin user created successfully!");
            Console.WriteLine($"  Email: {email}");
            Console.WriteLine($"  Password: {password}");
        }
        else
        {
            Console.WriteLine($"✗ Failed to create admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error.Description}");
            }
        }
    }
    return;
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
