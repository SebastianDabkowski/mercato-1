# Copilot Instructions for Mercato

This file provides repository-specific instructions for GitHub Copilot to assist with development in the Mercato project.

## Project Overview

Mercato is a multi-vendor e-commerce marketplace platform built with .NET 9, ASP.NET Core Razor Pages, and Entity Framework Core. The platform connects independent online stores with buyers and features distinct interfaces for Buyers, Sellers, and Administrators.

## Architecture

This solution uses a **modular monolith architecture** with clear separation of concerns:

### Layer Responsibilities
- **Domain**: Core entities, value objects, and repository interfaces. Business rules belong here.
- **Application**: Use cases and application services. Depends only on Domain interfaces.
- **Infrastructure**: EF Core implementations, external integrations, and persistence. Implements Domain interfaces.
- **UI (Razor Pages)**: Presentation layer. Calls Application services via dependency injection.

### Dependency Flow
```
Mercato.Web → Application (use cases) → Domain (interfaces)
                                              ↑
                              Infrastructure implements
```

### Project Structure
```
src/
├── Mercato.sln                  # Main solution file
├── Mercato.Web/                 # ASP.NET Core Web App (entry point)
│   ├── Data/                    # ApplicationDbContext for Identity
│   ├── Filters/                 # MVC filters
│   ├── Middleware/              # Custom middleware
│   ├── Pages/                   # Razor Pages organized by feature
│   └── Program.cs               # DI and startup configuration
├── Mercato.Identity/            # Identity module
├── Mercato.Seller/              # Seller module
├── Mercato.Buyer/               # Buyer module
├── Mercato.Product/             # Product module
├── Mercato.Cart/                # Cart module
├── Mercato.Orders/              # Orders module
├── Mercato.Payments/            # Payments module
├── Mercato.Admin/               # Admin module
└── Tests/
    ├── Mercato.Tests.Admin/     # Admin module tests
    ├── Mercato.Tests.Identity/  # Identity module tests
    ├── Mercato.Tests.Product/   # Product module tests
    └── Mercato.Tests.Seller/    # Seller module tests
```

### Module Structure
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

## Coding Standards

### General Guidelines
- Target: .NET 9, C# 13
- Follow .NET conventions (PascalCase for types/methods, camelCase for locals/parameters)
- Prefer small, focused methods and classes with clear responsibilities
- Use meaningful, intent-revealing names
- Avoid premature optimization and unnecessary abstractions
- Keep business logic out of controllers and infrastructure

### Architecture Rules
- Keep business rules in `Domain` layer
- Add application behavior in `Application` as small, testable services/use cases
- Keep EF Core and external integrations in `Infrastructure` only
- UI (Razor Pages) calls Application services via DI
- Avoid coupling Application to EF Core—use interfaces
- One `DbContext` per bounded context (e.g., Identity vs Products vs Cart vs Orders)

### CQRS Patterns
- Commands mutate state and return minimal data (often void or a result object)
- Queries do not change state and are optimized for read operations
- Use separate command and query handlers or services

### DDD Guidelines
- Model the ubiquitous language from the domain
- Use aggregates with clear invariants and explicit methods for behavior
- Prefer value objects for concepts with rules and validation
- Raise domain events when something important happens in the domain

### Result Pattern
- Result classes use `IsNotAuthorized` boolean flag for explicit authorization failures
- Result classes use empty collection initializer `[]` for `Errors` property in `Success()` methods
- Web handlers return `Forbid()` when `IsNotAuthorized` is true

### Validation Pattern
- Service layer validation uses private static helper methods that return `List<string>` of error messages
- Follow naming pattern `Validate[Entity/Command]` for validation methods

### Documentation
- All public classes, interfaces, properties, and methods in domain, application, and infrastructure layers use XML documentation comments

## Testing Requirements

### Testing Framework
- Use xUnit for unit tests
- Use Moq library for mocking interfaces with `MockBehavior.Strict`
- Follow Arrange–Act–Assert structure with clear test names

### Testing Practices
- Write unit tests in `Tests/Mercato.Tests.<ModuleName>` using mocks for interfaces
- For every significant business rule, create unit tests
- Cover edge cases and failure scenarios, not only the happy path
- Keep tests fast and independent from external services or databases
- Tests reference Application and Domain, mocking repository interfaces

### Test Location
- Add unit tests in `Tests/Mercato.Tests.Product` for Products module logic
- Add unit tests in `Tests/Mercato.Tests.Seller` for Seller module logic
- Add unit tests in `Tests/Mercato.Tests.Identity` for Identity module logic
- Add unit tests in `Tests/Mercato.Tests.Admin` for Admin module logic
- Mock repository interfaces (e.g., `IProductRepository`, `IStoreRepository`) to verify interactions

## Build and Development Commands

### Prerequisites
- .NET 9 SDK

### Build Commands
```bash
# Build the solution
cd src
dotnet build Mercato.sln

# Run the web application
cd Mercato.Web
dotnet run

# Run all tests
cd src
dotnet test Mercato.sln
```

### Database Migrations
```bash
# Add migration (from src directory)
dotnet ef migrations add <MigrationName> -p Mercato.Product -s Mercato.Web -c ProductDbContext

# Update database
dotnet ef database update -p Mercato.Product -s Mercato.Web -c ProductDbContext

# Other DbContexts follow the same pattern:
# ProductDbContext, CartDbContext, OrderDbContext, PaymentDbContext
```

## Adding New Features

When adding a new feature, follow this workflow:

1. **Domain**: Add entity changes, validations, and interface methods (e.g., `IProductRepository`)
2. **Infrastructure**: Implement the new method in the repository, update `DbContext` if needed, add EF Core migration if schema changes
3. **Application**: Create a new use case service that depends on repository interfaces
4. **Web UI**: Add Razor Page model injecting the new use case, add corresponding view
5. **DI Registration**: Register interfaces and use cases in the module's extension method (e.g., `ProductModuleExtensions.cs`)
6. **Tests**: Add unit tests mocking repository interfaces to verify interactions

## Module Registration

### Modules without persistence
```csharp
builder.Services.AddSellerModule();
builder.Services.AddBuyerModule();
builder.Services.AddIdentityModule();
builder.Services.AddAdminModule();
```

### Modules with persistence (require IConfiguration)
```csharp
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
```

## Boundaries and Constraints

### Do Not
- Introduce features marked out of scope for MVP or allocated for later phases
- Couple Application layer to EF Core—always use interfaces
- Put business logic in controllers or infrastructure
- Commit secrets or credentials
- Modify unrelated tests or functionality

### Always
- Respect user role boundaries (Buyer, Seller, Admin)
- Uphold GDPR requirements (data minimalism, audit logging, RBAC)
- Use async/await patterns everywhere (`Task`, `await`)
- Update DI registrations in the module's extension method when adding new services
- Add/adjust Razor Pages if the UI changes
- Ensure unit tests cover new code paths
- Validate image URLs using prefix allowlisting (restrict to trusted paths like `/uploads/` and `/images/`)

## Security Considerations

- Image URLs in views are validated using prefix allowlisting to restrict to trusted paths
- Result classes use `IsNotAuthorized` boolean flag for explicit authorization failures
- Always use `Forbid()` response in web handlers when authorization fails

## Documentation Requirements

- Update README.md for significant feature additions
- Update ARCHITECTURE.md for structural changes
- Add XML documentation comments for public APIs
- Include inline comments only for complex logic

## Code Review Checklist

Before submitting changes, verify:
- [ ] Domain API changes have corresponding Infrastructure updates
- [ ] Schema changes have migrations in the correct module and DbContext
- [ ] DI registrations are updated in the module's extension method
- [ ] Razor Pages are updated if UI changes
- [ ] Unit tests cover new functionality
- [ ] Code follows existing patterns and conventions
