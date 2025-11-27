# Copilot Instructions for Mercato

This file provides repository-specific instructions for GitHub Copilot to assist with development in the Mercato project.

## Project Overview

Mercato is a multi-vendor e-commerce marketplace platform built with .NET 9, ASP.NET Core Razor Pages, and Entity Framework Core. The platform connects independent online stores with buyers and features distinct interfaces for Buyers, Sellers, and Administrators.

## Architecture

This solution uses a modular, layered architecture with clear separation of concerns:

### Layer Responsibilities
- **Domain**: Core entities, value objects, and repository interfaces. Business rules belong here.
- **Application**: Use cases and application services. Depends only on Domain interfaces.
- **Infrastructure**: EF Core implementations, external integrations, and persistence. Implements Domain interfaces.
- **UI (Razor Pages)**: Presentation layer. Calls Application services via dependency injection.

### Dependency Flow
```
WebApp → Application (use cases) → Domain (interfaces)
Infrastructure implements Domain interfaces and is registered in DI at startup
```

### Project Structure
```
src/
├── Application/
│   └── SD.ProjectName.WebApp          # Razor Pages UI, app startup, DI, EF Core Identity
├── Modules/
│   └── SD.ProjectName.Modules.Products # Feature module (Domain, Application, Infrastructure)
├── Tests/
│   └── SD.ProjectName.Tests.Products   # Unit tests for Products module
└── SD.ProjectNameVertical.sln          # Solution file
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
- One `DbContext` per bounded context (e.g., Identity vs Products)

### CQRS Patterns
- Commands mutate state and return minimal data (often void or a result object)
- Queries do not change state and are optimized for read operations
- Use separate command and query handlers or services

### DDD Guidelines
- Model the ubiquitous language from the domain
- Use aggregates with clear invariants and explicit methods for behavior
- Prefer value objects for concepts with rules and validation
- Raise domain events when something important happens in the domain

## Testing Requirements

### Testing Framework
- Use xUnit for unit tests
- Use Moq library for mocking interfaces with `MockBehavior.Strict`
- Follow Arrange–Act–Assert structure with clear test names

### Testing Practices
- Write unit tests in `Tests/*` using mocks for interfaces
- For every significant business rule, create unit tests
- Cover edge cases and failure scenarios, not only the happy path
- Keep tests fast and independent from external services or databases
- Tests reference Application and Domain, mocking repository interfaces

### Test Location
- Add unit tests in `Tests/SD.ProjectName.Tests.Products` for Products module logic
- Mock `IProductRepository` to verify interactions

## Build and Development Commands

### Prerequisites
- .NET 9 SDK

### Build Commands
```bash
# Build the solution
cd src
dotnet build SD.ProjectNameVertical.sln

# Run the web application
cd Application/SD.ProjectName.WebApp
dotnet run

# Run all tests
cd src
dotnet test SD.ProjectNameVertical.sln
```

### Database Migrations
```bash
# Add migration (from src directory)
dotnet ef migrations add <MigrationName> -p Modules/SD.ProjectName.Modules.Products -s Application/SD.ProjectName.WebApp -c ProductDbContext

# Update database
dotnet ef database update -p Modules/SD.ProjectName.Modules.Products -s Application/SD.ProjectName.WebApp -c ProductDbContext
```

## Adding New Features

When adding a new feature, follow this workflow:

1. **Domain**: Add entity changes, validations, and interface methods to `IProductRepository`
2. **Infrastructure**: Implement the new method in `ProductRepository`, update `DbContext` if needed, add EF Core migration if schema changes
3. **Application**: Create a new use case service that depends on `IProductRepository`
4. **Web UI**: Add Razor Page model injecting the new use case, add corresponding view
5. **DI Registration**: Register interfaces and use cases in `Program.cs`
6. **Tests**: Add unit tests mocking `IProductRepository` to verify interactions

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
- Update DI registrations in `Program.cs` when adding new services
- Add/adjust Razor Pages if the UI changes
- Ensure unit tests cover new code paths

## Documentation Requirements

- Update README.md for significant feature additions
- Update ARCHITECTURE.md for structural changes
- Add XML documentation comments for public APIs
- Include inline comments only for complex logic

## Code Review Checklist

Before submitting changes, verify:
- [ ] Domain API changes have corresponding Infrastructure updates
- [ ] Schema changes have migrations in the correct project and DbContext
- [ ] DI registrations are updated in `Program.cs`
- [ ] Razor Pages are updated if UI changes
- [ ] Unit tests cover new functionality
- [ ] Code follows existing patterns and conventions
