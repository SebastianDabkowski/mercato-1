---
applyTo:
  - "**/Domain/**"
---

# Domain Layer Instructions

When working with files in the Domain layer, follow these guidelines:

## Purpose
The Domain layer contains core business entities, value objects, and repository interfaces. This is where business rules and domain logic belong.

## Guidelines

- **Entities**: Define core domain entities with meaningful properties and validation rules
- **Interfaces**: Define repository interfaces that Application layer depends on
- **Value Objects**: Use for concepts that have rules, validation, or are immutable
- **Business Rules**: Keep all business logic within domain entities and services

## Conventions

- Use XML documentation comments for all public members
- Entity classes should have private setters for properties to enforce invariants
- Use explicit methods for behavior rather than property setters
- Raise domain events when significant state changes occur

## Do Not

- Add any EF Core dependencies or attributes
- Reference Infrastructure or Application layers
- Add any external service integrations
