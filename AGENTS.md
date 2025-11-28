# AGENTS.md

This file provides agent-focused instructions for AI coding agents working on the Mercato project.

## Setup Commands

```bash
# Build the solution (from src directory)
cd src
dotnet build Mercato.sln

# Run the web application (from src directory)
cd Mercato.Web
dotnet run

# Run all tests (from src directory)
dotnet test Mercato.sln
```

## Code Style

- Target: .NET 9, C# 13
- Use PascalCase for types, methods, and public members
- Use camelCase for local variables and parameters
- Use async/await for all I/O operations
- Add XML documentation comments for public APIs
- Keep business logic in Domain layer, not controllers or infrastructure

## Testing Instructions

- Use xUnit for unit tests
- Use Moq library with `MockBehavior.Strict`
- Follow Arrange–Act–Assert structure
- Tests are located in `Tests/Mercato.Tests.<ModuleName>/`
  - `Tests/Mercato.Tests.Admin/` - Admin module tests
  - `Tests/Mercato.Tests.Identity/` - Identity module tests
  - `Tests/Mercato.Tests.Product/` - Product module tests
  - `Tests/Mercato.Tests.Seller/` - Seller module tests
- Mock repository interfaces rather than implementations
- Run tests with: `dotnet test Mercato.sln` from the `src` directory

## Project Structure

```
src/
├── Mercato.sln                  # Main solution file
├── Mercato.Web/                 # ASP.NET Core Web App (entry point)
├── Mercato.Identity/            # Identity module
├── Mercato.Seller/              # Seller module
├── Mercato.Buyer/               # Buyer module
├── Mercato.Product/             # Product module
├── Mercato.Cart/                # Cart module
├── Mercato.Orders/              # Orders module
├── Mercato.Payments/            # Payments module
├── Mercato.Admin/               # Admin module
└── Tests/                       # Unit tests per module
```

## Module Structure

Each module follows this layered architecture:

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

## PR Guidelines

- Domain API changes require corresponding Infrastructure updates
- Schema changes require migrations in the correct module and DbContext
- Update DI registrations in the module's extension method when adding new services
- Ensure unit tests cover new code paths
- Follow existing patterns and conventions

## Boundaries

- Do not introduce features marked out of scope for MVP
- Do not couple Application layer to EF Core—always use interfaces
- Do not put business logic in controllers or infrastructure
- Do not commit secrets or credentials
- Do not modify unrelated tests or functionality
- Respect user role boundaries (Buyer, Seller, Admin)
