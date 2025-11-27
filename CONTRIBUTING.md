# Contributing to Mercato

Thank you for your interest in contributing to Mercato! This document provides guidelines and instructions for contributing to this project.

## Getting Started

### Prerequisites
- .NET 9 SDK
- Git

### Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/SebastianDabkowski/mercato-1.git
   cd mercato-1
   ```

2. Build the solution:
   ```bash
   cd src
   dotnet build Mercato.sln
   ```

3. Run tests to verify setup:
   ```bash
   dotnet test Mercato.sln
   ```

4. Run the web application:
   ```bash
   cd Mercato.Web
   dotnet run
   ```

## Solution Structure

Mercato uses a modular monolith architecture. The solution is organized as follows:

```
src/
├── Mercato.sln                  # Main solution file
├── Mercato.Web/                 # ASP.NET Core Web App (entry point)
│   ├── Pages/                   # Razor Pages organized by feature
│   ├── Data/                    # ApplicationDbContext for Identity
│   ├── Middleware/              # Custom middleware (e.g., exception handling)
│   ├── Filters/                 # MVC filters (e.g., request validation)
│   └── Program.cs               # DI and startup configuration
├── Mercato.Identity/            # Identity module
├── Mercato.Seller/              # Seller module
├── Mercato.Buyer/               # Buyer module
├── Mercato.Product/             # Product module
├── Mercato.Cart/                # Cart module
├── Mercato.Orders/              # Orders module
├── Mercato.Payments/            # Payments module
└── Mercato.Admin/               # Admin module
```

### Mercato.Web (Entry Point)

`Mercato.Web` is the **sole ASP.NET Core web project** that hosts the UI and startup logic. Responsibilities include:

- **Program.cs**: Configures DI, registers all modules, sets up middleware
- **Razor Pages**: UI pages organized by feature folder (Admin, Buyer, Cart, Orders, Product, Seller)
- **Identity DbContext**: `ApplicationDbContext` for ASP.NET Core Identity
- **Role Seeding**: `RoleSeeder` seeds Buyer, Seller, Admin roles at startup
- **Middleware**: Global exception handling
- **Filters**: Request validation

### Module Projects

Each module follows the naming convention `Mercato.<ModuleName>` and is a class library with this structure:

```
Mercato.<ModuleName>/
├── Domain/
│   ├── Entities/                # Core domain entities
│   └── Interfaces/              # Repository and service interfaces
├── Application/
│   ├── Commands/                # Command handlers (CQRS)
│   ├── Queries/                 # Query handlers (CQRS)
│   ├── Services/                # Service interfaces
│   └── UseCases/                # Application use cases
├── Infrastructure/
│   ├── Persistence/             # DbContext and EF Core configurations
│   └── Repositories/            # Repository implementations
└── <ModuleName>ModuleExtensions.cs  # DI registration extension method
```

## Dependency Injection

### Global DI Configuration

All DI configuration is in `Mercato.Web/Program.cs`. Each module provides an extension method to register its services.

### Module Registration Pattern

Modules that don't require persistence:
```csharp
// In Mercato.Seller/SellerModuleExtensions.cs
public static IServiceCollection AddSellerModule(this IServiceCollection services)
{
    // Register services
    return services;
}
```

Modules that require persistence accept `IConfiguration`:
```csharp
// In Mercato.Product/ProductModuleExtensions.cs
public static IServiceCollection AddProductModule(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    services.AddDbContext<ProductDbContext>(options =>
        options.UseSqlServer(connectionString));
    // Register repositories and services
    return services;
}
```

### Registering Modules in Program.cs

```csharp
// Modules without persistence
builder.Services.AddSellerModule();
builder.Services.AddBuyerModule();
builder.Services.AddIdentityModule();
builder.Services.AddAdminModule();

// Modules with persistence (pass configuration)
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
```

### Adding New Services

When adding a new service:
1. Define the interface in `Domain/Interfaces/`
2. Implement in `Infrastructure/` or `Application/`
3. Register in the module's `Add<ModuleName>Module()` method

## Identity & Authentication

### Configuration Location

Identity is configured in `Mercato.Web/Program.cs`:

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password, lockout, and user settings
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
```

### Role Seeding

Roles are seeded at startup in `Mercato.Web/Data/RoleSeeder.cs`:
- `Buyer` - For buyer users
- `Seller` - For seller users
- `Admin` - For administrator users

### Adding Authentication Features

For new authentication features, add them to the `Mercato.Identity` module. The module exposes services via `AddIdentityModule()`.

## DbContexts

### Overview

The platform uses a **shared database** with **separate DbContext per module**:

| Module | DbContext | Location |
|--------|-----------|----------|
| Identity (Web) | `ApplicationDbContext` | `Mercato.Web/Data/` |
| Product | `ProductDbContext` | `Mercato.Product/Infrastructure/Persistence/` |
| Cart | `CartDbContext` | `Mercato.Cart/Infrastructure/Persistence/` |
| Orders | `OrderDbContext` | `Mercato.Orders/Infrastructure/Persistence/` |
| Payments | `PaymentDbContext` | `Mercato.Payments/Infrastructure/Persistence/` |

### Connection String

All DbContexts use the `DefaultConnection` connection string from `appsettings.json`.

### Database Migrations

Run migrations from the `src` directory:

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  -p Mercato.Product \
  -s Mercato.Web \
  -c ProductDbContext

# Apply migrations
dotnet ef database update \
  -p Mercato.Product \
  -s Mercato.Web \
  -c ProductDbContext
```

## UI Organization

### Razor Pages Structure

Pages are organized by feature in `Mercato.Web/Pages/`:

```
Pages/
├── Index.cshtml                 # Home page
├── Shared/
│   └── _Layout.cshtml           # Main layout template
├── Admin/                       # Admin portal pages
├── Buyer/                       # Buyer portal pages
├── Cart/                        # Shopping cart pages
├── Orders/                      # Order management pages
├── Product/                     # Product catalog pages
└── Seller/                      # Seller portal pages
```

### Layout Conventions

- Use `_Layout.cshtml` as the base layout
- Bootstrap 5 is the standard CSS framework
- Use `@ViewData["Title"]` for page titles
- Use Razor comment syntax `@* TODO: ... *@` for TODO comments

### Page Model Conventions

```csharp
public class ExampleModel : PageModel
{
    private readonly IExampleService _service;

    public ExampleModel(IExampleService service)
    {
        _service = service;
    }

    public async Task OnGetAsync()
    {
        // Use async handlers
    }
}
```

### Authorization

Apply authorization attributes to pages requiring authentication:

```csharp
[Authorize(Roles = "Seller")]
public class SellerDashboardModel : PageModel { }
```

## Development Workflow

### Branch Strategy
- Create feature branches from `main`
- Use descriptive branch names (e.g., `feature/add-product-search`, `fix/cart-calculation`)

### Making Changes
1. Create a new branch for your changes
2. Make your changes following the coding standards
3. Write or update tests for your changes
4. Ensure all tests pass locally
5. Submit a pull request

### Pull Request Guidelines
- Provide a clear description of the changes
- Reference any related issues
- Ensure CI checks pass
- Request review from maintainers

## Coding Standards

### C# Conventions
- Target: .NET 9, C# 13
- Follow .NET naming conventions:
  - PascalCase for types, methods, and public members
  - camelCase for local variables and parameters
- Write clear, self-documenting code
- Keep methods and classes focused and small

### Architecture Rules
- Keep business rules in Domain layer
- Use interfaces for dependencies
- Avoid coupling Application to EF Core
- One DbContext per bounded context

### Async Patterns
- Use `async/await` everywhere for I/O operations
- Use `Task` return types for async methods
- Name async methods with `Async` suffix (e.g., `GetProductsAsync`)

### Testing Requirements
- Use xUnit for unit tests
- Use Moq with `MockBehavior.Strict`
- Write tests for all significant business logic
- Cover edge cases and error scenarios
- Keep tests fast and independent

## Adding New Features

Follow this workflow when adding features:

1. **Domain Layer**
   - Add/update entities in `Domain/Entities/`
   - Define repository interfaces in `Domain/Interfaces/`

2. **Infrastructure Layer**
   - Implement repository interfaces in `Infrastructure/Repositories/`
   - Add EF Core configurations if needed
   - Create migrations for schema changes

3. **Application Layer**
   - Create use case classes in `Application/UseCases/`
   - Or use Commands/Queries pattern in respective folders
   - Keep logic testable and independent of EF Core

4. **UI Layer (Mercato.Web)**
   - Add Razor Pages in appropriate folder
   - Inject application services via DI

5. **DI Registration**
   - Register new services in the module's extension method

6. **Tests**
   - Add unit tests with mocked interfaces
   - Follow Arrange-Act-Assert pattern

## Scope Guidelines

### In Scope for MVP
- Identity & Access Management
- Seller Management
- Product Catalog Management
- Product Search & Navigation
- Shopping Cart & Checkout
- Orders & Fulfilment
- Payments & Settlements
- Basic Returns
- Administration basics

### Out of Scope for MVP
- Advanced analytics
- Shipping integrations
- Product variants
- Promo codes
- Public API

Please refer to [prd.md](prd.md) for detailed requirements.

## User Roles

The platform supports three user roles:
- **Buyer**: Browse, search, purchase products
- **Seller**: Manage stores, products, orders
- **Admin**: Platform management and moderation

Respect role boundaries when implementing features.

## Compliance Requirements

- Follow GDPR guidelines (data minimalism, audit logging)
- Implement proper RBAC for all features
- Never commit secrets or credentials

## Getting Help

- Review existing documentation in README.md and ARCHITECTURE.md
- Check the Product Requirements Document (prd.md)
- Open an issue for questions or clarifications

## Code of Conduct

Please be respectful and constructive in all interactions. We are committed to maintaining a welcoming and inclusive environment for all contributors.
