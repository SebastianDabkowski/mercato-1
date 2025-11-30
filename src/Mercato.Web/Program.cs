using Mercato.Web.Data;
using Mercato.Web.Filters;
using Mercato.Web.Middleware;
using Mercato.Admin;
using Mercato.Analytics;
using Mercato.Buyer;
using Mercato.Buyer.Infrastructure;
using Mercato.Buyer.Infrastructure.Persistence;
using Mercato.Cart;
using Mercato.Identity;
using Mercato.Notifications;
using Mercato.Orders;
using Mercato.Payments;
using Mercato.Product;
using Mercato.Seller;
using Mercato.Shipping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // TODO: Add Razor Pages-specific configuration (e.g., conventions, filters)
}).AddMvcOptions(options =>
{
    // Register request validation filter globally
    options.AddRequestValidationFilter();
});

// Configure Entity Framework Core with SQL Server for Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings (using defaults, can be customized)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication
var sessionTimeoutMinutes = builder.Configuration.GetValue<int>("Session:InactivityTimeoutMinutes", 60);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Configure Google OAuth authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

// Configure Facebook OAuth authentication
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.CallbackPath = "/signin-facebook";
    });
}

// Register module services
builder.Services.AddSellerModule(builder.Configuration);
builder.Services.AddBuyerModule(builder.Configuration);
builder.Services.AddProductModule(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddShippingModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddIdentityModule();
builder.Services.AddAdminModule(builder.Configuration);
builder.Services.AddAnalyticsModule(builder.Configuration);

var app = builder.Build();

// TODO: FIX it: RoleSeeder runs before ensuring database exists or is migrated.
// This causes startup failure if database is not pre-created. Consider:
// 1. Adding EnsureCreated/Migrate logic before seeding, or
// 2. Moving role seeding to a separate initialization step, or
// 3. Adding try-catch to gracefully handle database unavailability at startup.
// Seed roles and consent types
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedRolesAsync(roleManager);

    // Seed consent types
    var buyerDbContext = scope.ServiceProvider.GetRequiredService<BuyerDbContext>();
    await ConsentSeeder.SeedConsentTypesAsync(buyerDbContext);
}

// Configure the HTTP request pipeline.

// Global exception handling - logs all exceptions and handles API errors with JSON response.
// For non-API (page) requests, exceptions are re-thrown and handled by UseExceptionHandler below.
app.UseGlobalExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    // Handles page request exceptions by redirecting to error page
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
