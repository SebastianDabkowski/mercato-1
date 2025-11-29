---
applyTo:
  - "**/Application/**"
---

# Application Layer Instructions

When working with files in the Application layer, follow these guidelines:

## Purpose
The Application layer contains use cases and application services. It orchestrates domain objects and depends only on Domain interfaces.

## Guidelines

- **Use Cases**: Create small, focused classes for each use case
- **Commands**: Use for operations that mutate state, return minimal data
- **Queries**: Use for read operations, do not change state
- **Services**: Define service interfaces for complex orchestration

## Conventions

- Use XML documentation comments for all public members
- Follow CQRS pattern with Commands and Queries separation
- Use async/await for all I/O operations
- Return Result objects with success/failure indication
- Use `IsNotAuthorized` flag for authorization failures
- Validation methods should be named `Validate[Entity/Command]`

## Result Pattern

```csharp
public static Result Success() => new() { Errors = [] };
public bool IsNotAuthorized { get; init; }
```

## Do Not

- Reference EF Core or Infrastructure directly
- Put business rules here (they belong in Domain)
- Access external services directly (use interfaces)
