---
applyTo:
  - "**/Infrastructure/**"
---

# Infrastructure Layer Instructions

When working with files in the Infrastructure layer, follow these guidelines:

## Purpose
The Infrastructure layer contains EF Core implementations, external integrations, and persistence logic. It implements Domain interfaces.

## Guidelines

- **Repositories**: Implement Domain repository interfaces using EF Core
- **DbContext**: One DbContext per bounded context
- **Persistence**: Configure entity mappings and migrations

## Conventions

- Use XML documentation comments for all public members
- Use async/await for all database operations
- Implement interfaces defined in Domain layer
- Configure entities using Fluent API in DbContext

## DbContext Pattern

```csharp
services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(connectionString));
```

## Migrations

From the `src` directory:
```bash
dotnet ef migrations add <MigrationName> -p Mercato.<Module> -s Mercato.Web -c <Module>DbContext
dotnet ef database update -p Mercato.<Module> -s Mercato.Web -c <Module>DbContext
```

## Do Not

- Put business logic here (it belongs in Domain)
- Expose EF Core types to Application layer
- Create cross-module Infrastructure dependencies
