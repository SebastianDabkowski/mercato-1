# Architecture Overview

Mercato is a multi-vendor e-commerce marketplace built using a **modular monolith architecture** with ASP.NET Core Razor Pages and Entity Framework Core. This document describes the technical structure and conventions of the codebase.

---

## Solution Structure

The solution follows the `Mercato.<ModuleName>` naming convention:

```
src/
├── Mercato.sln                  # Main solution file
│
├── Mercato.Web/                 # ASP.NET Core Web App (entry point)
│   ├── Data/                    # ApplicationDbContext for Identity
│   │   ├── ApplicationDbContext.cs
│   │   └── RoleSeeder.cs
│   ├── Filters/                 # MVC filters
│   │   └── RequestValidationFilter.cs
│   ├── Middleware/              # Custom middleware
│   │   └── GlobalExceptionHandlerMiddleware.cs
│   ├── Pages/                   # Razor Pages organized by feature
│   │   ├── Admin/
│   │   ├── Buyer/
│   │   ├── Cart/
│   │   ├── Orders/
│   │   ├── Product/
│   │   ├── Seller/
│   │   ├── Shared/
│   │   │   └── _Layout.cshtml
│   │   └── Index.cshtml
│   ├── Program.cs               # DI and startup configuration
│   └── appsettings.json         # Configuration
│
├── Mercato.Identity/            # Identity module
├── Mercato.Seller/              # Seller module
├── Mercato.Buyer/               # Buyer module
├── Mercato.Product/             # Product module
├── Mercato.Cart/                # Cart module
├── Mercato.Orders/              # Orders module
├── Mercato.Payments/            # Payments module
└── Mercato.Admin/               # Admin module
```

---

## Mercato.Web (Entry Point)

`Mercato.Web` is the **sole ASP.NET Core web project** that hosts the UI and startup logic.

### Responsibilities

| Component | Purpose |
|-----------|---------|
| `Program.cs` | DI configuration, module registration, middleware setup |
| `Data/ApplicationDbContext.cs` | EF Core Identity DbContext |
| `Data/RoleSeeder.cs` | Seeds Buyer, Seller, Admin roles at startup |
| `Pages/` | Razor Pages UI organized by feature |
| `Middleware/` | Global exception handling |
| `Filters/` | Request validation |

### Module Registration

All modules are registered in `Program.cs` via extension methods:

```csharp
// Modules without persistence
builder.Services.AddSellerModule();
builder.Services.AddBuyerModule();
builder.Services.AddIdentityModule();
builder.Services.AddAdminModule();

// Modules with persistence (require IConfiguration)
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
```

---

## Module Architecture

Each module follows a consistent layered architecture:

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
│   ├── Persistence/             # DbContext, EF Core configurations
│   └── Repositories/            # Repository implementations
└── <ModuleName>ModuleExtensions.cs  # DI registration
```

### Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Core entities, value objects, repository interfaces. Business rules belong here. |
| **Application** | Use cases and application services. Depends only on Domain interfaces. |
| **Infrastructure** | EF Core implementations, external integrations. Implements Domain interfaces. |

### Dependency Flow

```
Mercato.Web → Application (use cases) → Domain (interfaces)
                                              ↑
                              Infrastructure implements
```

---

## Module to Project Mapping

| Module | Project Name | Primary Responsibility |
|--------|--------------|------------------------|
| Web | `Mercato.Web` | UI and hosting (entry point), Razor Pages, app startup, DI configuration |
| Identity | `Mercato.Identity` | Authentication, authorization, user management, RBAC |
| Seller | `Mercato.Seller` | Seller accounts, store management, onboarding, payout settings |
| Buyer | `Mercato.Buyer` | Buyer profiles, preferences, purchase history, wish lists |
| Product | `Mercato.Product` | Product catalog, categories, attributes, inventory |
| Cart | `Mercato.Cart` | Shopping cart, promotions, cart updates, multi-seller support |
| Orders | `Mercato.Orders` | Order lifecycle, fulfillment, tracking, returns |
| Payments | `Mercato.Payments` | Payment processing, refunds, settlements, commission calculations |
| Admin | `Mercato.Admin` | Platform administration, moderation, reporting, audit logging |

---

## Dependency Injection

### Module Registration Pattern

Each module provides an extension method for DI registration:

**Modules without persistence:**
```csharp
// Mercato.Seller/SellerModuleExtensions.cs
public static class SellerModuleExtensions
{
    public static IServiceCollection AddSellerModule(this IServiceCollection services)
    {
        // Register services and repositories
        return services;
    }
}
```

**Modules with persistence (require IConfiguration):**
```csharp
// Mercato.Product/ProductModuleExtensions.cs
public static class ProductModuleExtensions
{
    public static IServiceCollection AddProductModule(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ProductDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories and services
        return services;
    }
}
```

### Adding New Services

1. Define the interface in `Domain/Interfaces/`
2. Implement in `Infrastructure/Repositories/` or `Application/Services/`
3. Register in the module's `Add<ModuleName>Module()` method

---

## Identity & Authentication

### Configuration

Identity is configured in `Mercato.Web/Program.cs`:

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

### Cookie Authentication

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});
```

### User Roles

Three roles are seeded at startup via `RoleSeeder`:
- **Buyer** - For buyer users
- **Seller** - For seller users  
- **Admin** - For administrator users

### Social Login (OAuth)

Buyers can log in using external OAuth providers:

- **Google**: Enabled by configuring `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret`
- **Facebook**: Enabled by configuring `Authentication:Facebook:AppId` and `Authentication:Facebook:AppSecret`

Social login services are implemented in `Mercato.Identity`:
- `IGoogleLoginService` / `GoogleLoginService`
- `IFacebookLoginService` / `FacebookLoginService`

### Extending Authentication

For new authentication features (e.g., additional social login providers, 2FA), add them to `Mercato.Identity` and expose via `AddIdentityModule()`.

---

## Data Access Strategy

### Shared Database, Separate DbContexts

The platform uses a **shared database instance** with **separate DbContext classes** per module:

| Module | DbContext | Location |
|--------|-----------|----------|
| Identity (Web) | `ApplicationDbContext` | `Mercato.Web/Data/` |
| Product | `ProductDbContext` | `Mercato.Product/Infrastructure/Persistence/` |
| Cart | `CartDbContext` | `Mercato.Cart/Infrastructure/Persistence/` |
| Orders | `OrderDbContext` | `Mercato.Orders/Infrastructure/Persistence/` |
| Payments | `PaymentDbContext` | `Mercato.Payments/Infrastructure/Persistence/` |

### Connection String

All DbContexts use the `DefaultConnection` connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=Mercato;..."
  }
}
```

### Database Migrations

Run from the `src` directory:

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

### Benefits

- **Module Isolation**: Each module manages its own entity configurations and migrations
- **Clear Boundaries**: Each DbContext only exposes entities relevant to its bounded context
- **Simplified Development**: No cross-module EF Core dependencies at the DbContext level

---

## UI Organization

### Razor Pages Structure

Pages are organized by feature in `Mercato.Web/Pages/`:

```
Pages/
├── Index.cshtml                 # Home page
├── _ViewImports.cshtml          # Global using directives
├── _ViewStart.cshtml            # Layout configuration
├── Shared/
│   └── _Layout.cshtml           # Main layout template
├── Admin/                       # Admin portal pages
│   ├── Index.cshtml
│   └── Details.cshtml
├── Buyer/                       # Buyer portal pages
├── Cart/                        # Shopping cart pages
├── Orders/                      # Order management pages
├── Product/                     # Product catalog pages
└── Seller/                      # Seller portal pages
```

### Layout Conventions

- **Base Layout**: `Pages/Shared/_Layout.cshtml` - Bootstrap 5 based
- **Page Titles**: Use `@ViewData["Title"]` 
- **Comments**: Use Razor syntax `@* TODO: ... *@`

### Page Model Pattern

```csharp
public class ExampleModel : PageModel
{
    private readonly IExampleService _service;

    public ExampleModel(IExampleService service)
    {
        _service = service;
    }

    // Use async handlers
    public async Task OnGetAsync()
    {
        // Delegate work to Application services
    }
}
```

### Authorization

Apply authorization to pages using attributes:

```csharp
[Authorize(Roles = "Seller")]
public class SellerDashboardModel : PageModel { }

[Authorize(Roles = "Admin")]
public class AdminPanelModel : PageModel { }
```

---

## Adding New Features

Follow this workflow when adding new functionality:

### 1. Domain Layer

Add or update entities and interfaces:
```
Mercato.<ModuleName>/Domain/
├── Entities/
│   └── NewEntity.cs           # Add new entity
└── Interfaces/
    └── INewEntityRepository.cs  # Define repository interface
```

### 2. Infrastructure Layer

Implement the repository and update DbContext:
```
Mercato.<ModuleName>/Infrastructure/
├── Persistence/
│   └── <ModuleName>DbContext.cs  # Add DbSet, configure entity
└── Repositories/
    └── NewEntityRepository.cs     # Implement interface
```

Add migration if schema changes:
```bash
dotnet ef migrations add AddNewEntity \
  -p Mercato.<ModuleName> \
  -s Mercato.Web \
  -c <ModuleName>DbContext
```

### 3. Application Layer

Create use cases or commands/queries:
```
Mercato.<ModuleName>/Application/
├── UseCases/
│   └── CreateNewEntity.cs       # Or use Commands/Queries
└── Services/
    └── INewEntityService.cs
```

### 4. DI Registration

Register services in the module's extension method:
```csharp
public static IServiceCollection Add<ModuleName>Module(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // Register DbContext...
    
    // Register repository
    services.AddScoped<INewEntityRepository, NewEntityRepository>();
    
    // Register use case
    services.AddScoped<CreateNewEntity>();
    
    return services;
}
```

### 5. UI Layer (Mercato.Web)

Add Razor Pages:
```
Mercato.Web/Pages/<Feature>/
├── Create.cshtml
└── Create.cshtml.cs
```

### 6. Tests

Add unit tests mocking repository interfaces:
```csharp
[Fact]
public async Task CreateNewEntity_ValidInput_ReturnsSuccess()
{
    // Arrange
    var mockRepo = new Mock<INewEntityRepository>(MockBehavior.Strict);
    mockRepo.Setup(r => r.AddAsync(It.IsAny<NewEntity>()))
            .ReturnsAsync(expectedEntity);
    
    var useCase = new CreateNewEntity(mockRepo.Object);
    
    // Act
    var result = await useCase.ExecuteAsync(input);
    
    // Assert
    Assert.NotNull(result);
}
```

---

## Architecture Principles

### General Rules

- Keep business rules in **Domain** layer
- Add application behavior in **Application** as small, testable services/use cases
- Keep EF Core and external integrations in **Infrastructure**
- UI (Razor Pages) calls Application services via DI

### CQRS Pattern

- **Commands**: Mutate state, return minimal data (void or result object)
- **Queries**: Read-only, optimized for read operations

### DDD Guidelines

- Model the ubiquitous language from the domain
- Use aggregates with clear invariants and explicit methods
- Prefer value objects for concepts with rules and validation
- Raise domain events when significant state changes occur

### Async Patterns

- Use `async/await` everywhere for I/O operations
- Use `Task` return types for async methods
- Prefer small, composable services

---

## Inter-Module Communication

### Synchronous (DI)

Use for queries where immediate response is needed:
```csharp
// Module exposes interface in Domain/Interfaces
public interface IProductService
{
    Task<Product> GetByIdAsync(int id);
}

// Consuming module injects via DI
public class OrderService
{
    private readonly IProductService _productService;
    
    public OrderService(IProductService productService)
    {
        _productService = productService;
    }
}
```

### Asynchronous (Domain Events)

Use for notifications and eventual consistency:
```
[Orders Module] --> OrderCreated event
    ├── [Notifications Module] --> Send confirmation email
    └── [Admin Module] --> Log audit entry
```

---

## Dependency Rules

### Allowed Dependencies

```
Mercato.Web → Any module's Application layer
Module Application → Same module's Domain
Module Infrastructure → Same module's Domain
Module A Application → Module B Domain (cross-module queries)
```

### Prohibited Dependencies

```
Domain → Application (inverts dependency)
Domain → Infrastructure (inverts dependency)
Application → Infrastructure (should use interfaces)
Module A Infrastructure → Module B Infrastructure (tight coupling)
```

---

## Coding Conventions

### Naming

- Target: .NET 9, C# 13
- PascalCase for types, methods, public members
- camelCase for local variables, parameters

### Testing

- Use xUnit for unit tests
- Use Moq with `MockBehavior.Strict`
- Follow Arrange-Act-Assert pattern
- Mock repository interfaces, not implementations

### Performance

- Query shaping in repository implementations
- Use async everywhere (`Task`, `await`)
- Prefer small, composable services

---

## Checklist When Modifying

- [ ] Domain API changes require Infrastructure updates
- [ ] Schema changes require migrations in the correct module/DbContext
- [ ] Update DI registrations in module extension method
- [ ] Add/adjust Razor Pages if UI changes
- [ ] Ensure unit tests cover new code paths

---

## Key File Locations

| Purpose | Location |
|---------|----------|
| Web Entry Point | `src/Mercato.Web/` |
| DI Configuration | `src/Mercato.Web/Program.cs` |
| Identity DbContext | `src/Mercato.Web/Data/ApplicationDbContext.cs` |
| Role Seeder | `src/Mercato.Web/Data/RoleSeeder.cs` |
| Module Template | `src/Mercato.<ModuleName>/` |
| Module DI Extension | `src/Mercato.<ModuleName>/<ModuleName>ModuleExtensions.cs` |
| Module DbContext | `src/Mercato.<ModuleName>/Infrastructure/Persistence/` |
| Solution File | `src/Mercato.sln` |

---

## Notes

- Target: .NET 9, C# 13
- Prefer minimal changes that follow existing patterns
- Always use async/await for I/O operations
- One DbContext per bounded context
