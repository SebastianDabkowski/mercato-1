---
applyTo:
  - "**/Tests/**"
---

# Test Layer Instructions

When working with test files, follow these guidelines:

## Purpose
Tests validate business logic and application behavior using mocked interfaces.

## Framework

- **xUnit**: Use for all unit tests
- **Moq**: Use with `MockBehavior.Strict` for mocking

## Test Structure

Follow Arrange-Act-Assert pattern:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var mockRepo = new Mock<IRepository>(MockBehavior.Strict);
    mockRepo.Setup(r => r.Method(It.IsAny<Type>()))
            .ReturnsAsync(expectedValue);
    
    var service = new Service(mockRepo.Object);
    
    // Act
    var result = await service.ExecuteAsync(input);
    
    // Assert
    Assert.NotNull(result);
    mockRepo.VerifyAll();
}
```

## Test Location

- `Tests/Mercato.Tests.Product/` - Product module tests
- `Tests/Mercato.Tests.Seller/` - Seller module tests
- `Tests/Mercato.Tests.Identity/` - Identity module tests
- `Tests/Mercato.Tests.Admin/` - Admin module tests

## Guidelines

- Test one behavior per test method
- Use descriptive test names that explain the scenario
- Cover edge cases and error scenarios
- Mock repository interfaces, not implementations
- Keep tests fast and independent

## Do Not

- Test EF Core or database directly (use integration tests)
- Share state between tests
- Modify unrelated tests
