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
   dotnet build SD.ProjectNameVertical.sln
   ```

3. Run tests to verify setup:
   ```bash
   dotnet test SD.ProjectNameVertical.sln
   ```

4. Run the web application:
   ```bash
   cd Application/SD.ProjectName.WebApp
   dotnet run
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

## Architecture Overview

Mercato follows a modular, layered architecture. Please read [ARCHITECTURE.md](ARCHITECTURE.md) for detailed guidance.

### Key Principles
- **Domain**: Core entities and repository interfaces
- **Application**: Use cases and application services
- **Infrastructure**: EF Core implementations and external integrations
- **UI**: Razor Pages presentation layer

### Adding New Features

Follow this workflow when adding features:

1. **Domain Layer**
   - Add/update entities in the module's `Domain` folder
   - Define repository interfaces in `Domain/Interfaces`

2. **Infrastructure Layer**
   - Implement repository interfaces
   - Add EF Core configurations if needed
   - Create migrations for schema changes

3. **Application Layer**
   - Create use case services
   - Keep logic testable and independent of EF Core

4. **UI Layer**
   - Add Razor Pages for new functionality
   - Inject application services via DI

5. **Tests**
   - Add unit tests using Moq
   - Mock repository interfaces
   - Follow Arrange-Act-Assert pattern

6. **DI Registration**
   - Register new services in `Program.cs`

## Coding Standards

### C# Conventions
- Use .NET 9 and C# 13 features appropriately
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

### Testing Requirements
- Use xUnit for unit tests
- Use Moq with `MockBehavior.Strict`
- Write tests for all significant business logic
- Cover edge cases and error scenarios
- Keep tests fast and independent

## Database Migrations

When making schema changes:

```bash
# Add a new migration (from src directory)
dotnet ef migrations add <MigrationName> \
  -p Modules/SD.ProjectName.Modules.Products \
  -s Application/SD.ProjectName.WebApp \
  -c ProductDbContext

# Apply migrations
dotnet ef database update \
  -p Modules/SD.ProjectName.Modules.Products \
  -s Application/SD.ProjectName.WebApp \
  -c ProductDbContext
```

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
