---
applyTo:
  - "**/Pages/**"
  - "**/*.cshtml"
  - "**/*.cshtml.cs"
---

# Razor Pages Instructions

When working with Razor Pages, follow these guidelines:

## Purpose
Razor Pages handle UI presentation and call Application services via dependency injection.

## Page Organization

Pages are organized by feature in `Mercato.Web/Pages/`:
- `Admin/` - Admin portal pages
- `Buyer/` - Buyer portal pages
- `Cart/` - Shopping cart pages
- `Orders/` - Order management pages
- `Product/` - Product catalog pages
- `Seller/` - Seller portal pages

## Page Model Pattern

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
        // Delegate work to Application services
    }
}
```

## Authorization

Apply authorization using attributes:

```csharp
[Authorize(Roles = "Seller")]
public class SellerDashboardModel : PageModel { }

[Authorize(Roles = "Admin")]
public class AdminPanelModel : PageModel { }
```

## Result Handling

```csharp
if (result.IsNotAuthorized)
{
    return Forbid();
}
```

## Layout Conventions

- Use `_Layout.cshtml` as base layout
- Set page title with `@ViewData["Title"]`
- Use Razor comments `@* TODO: ... *@`
- Bootstrap 5 is the standard CSS framework

## Security

- Validate image URLs using prefix allowlisting
- Restrict to trusted paths like `/uploads/` and `/images/`

## Do Not

- Put business logic in page models
- Access repositories directly (use Application services)
