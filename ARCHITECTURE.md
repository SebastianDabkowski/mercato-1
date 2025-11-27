# Architecture Overview

This solution uses a modular, layered architecture with a Razor Pages Web App and a feature module for Products.

---

# Mercato Naming Convention and Project Layout

> **IMPORTANT: Planning Phase Only**
>
> This section documents the naming convention and project layout for the Mercato modular monolith. **No physical projects or folders should be created yet.** This is a planning phase only, intended to establish consensus on the naming convention and structure before implementation.

## Naming Convention

The Mercato solution follows a simple, consistent naming convention for all projects:

- **Entry Point (ASP.NET Core Web Project):** `Mercato.Web`
- **Module Class Libraries:** `Mercato.<ModuleName>` (e.g., `Mercato.Identity`, `Mercato.Product`)

This convention prioritizes:
- Simplicity and readability
- Clear identification of the solution (all projects start with `Mercato.`)
- Short, meaningful module names that align with business domains

## Module to Project Mapping

The following table maps each module to its corresponding project name:

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

## Proposed Solution Structure

```
Mercato.sln
├── Mercato.Web             # ASP.NET Core Web App (entry point)
│   ├── Pages/              # Razor Pages
│   ├── Program.cs          # DI and startup configuration
│   └── appsettings.json    # Configuration files
├── Mercato.Identity/       # Identity module (class library)
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Mercato.Seller/         # Seller module (class library)
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Mercato.Buyer/          # Buyer module (class library)
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Mercato.Product/        # Product module (class library)
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Mercato.Cart/           # Cart module (class library)
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Mercato.Orders/         # Orders module (class library)
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── Mercato.Payments/       # Payments module (class library)
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
└── Mercato.Admin/          # Admin module (class library)
    ├── Domain/
    ├── Application/
    └── Infrastructure/
```

## Module Internal Structure

Each module class library follows a consistent internal layered architecture:

```
Mercato.<ModuleName>/
├── Domain/
│   ├── Entities/           # Core domain entities
│   └── Interfaces/         # Repository and service interfaces
├── Application/
│   └── UseCases/           # Application services and use cases
└── Infrastructure/
    ├── Repositories/       # Repository implementations
    └── Persistence/        # DbContext, migrations, EF Core configurations
```

## Key Principles

1. **Single Entry Point:** `Mercato.Web` is the sole ASP.NET Core web project that hosts all UI and startup logic.
2. **Module Isolation:** Each module is a separate class library with its own Domain, Application, and Infrastructure layers.
3. **Dependency Flow:** `Web → Application (use cases) → Domain (interfaces)`. Infrastructure implements Domain interfaces and is registered in DI at startup.
4. **Separate DbContexts:** Each module maintains its own `DbContext` for bounded context isolation.
5. **Best Practices:** Follow .NET modular monolith best practices, including CQRS patterns, DDD guidelines, and async/await patterns.

---

## Projects
- `Application/SD.ProjectName.WebApp`: Razor Pages UI, app startup, DI, EF Core Identity.
- `Modules/SD.ProjectName.Modules.Products`: Feature module containing `Domain`, `Application`, `Infrastructure` for Products.
- `Tests/SD.ProjectName.Tests.Products`: Unit tests for module/application logic.

## Module Structure
- `Domain`: Core entities and repository interfaces.
  - `ProductModel`
  - `Interfaces/IProductRepository`
- `Application`: Use cases (application services) depending on `Domain` interfaces.
  - `GetProducts`
- `Infrastructure`: Implementations and persistence for the module.
  - `ProductDbContext` + `Migrations`
  - `ProductRepository` (implements `IProductRepository`)

## Data
- Web App has `ApplicationDbContext` for Identity.
- Products module has its own `ProductDbContext` with separate migrations.

## Dependency Flow
- WebApp -> Application (use cases) -> Domain (interfaces)
- Infrastructure implements Domain interfaces and is registered in DI at startup.
- Tests reference Application and Domain, mocking `IProductRepository`.

## DI and Startup
- `Program.cs` configures:
  - Razor Pages
  - Identity `ApplicationDbContext`
  - Products module `ProductDbContext`
  - Registers `IProductRepository` with `ProductRepository`
  - Registers application services like `GetProducts`

## UI Pages (Razor Pages)
- Pages in `Application/SD.ProjectName.WebApp/Pages/*`
- Example: `Pages/Products/List.cshtml` + `List.cshtml.cs` consumes `GetProducts` via DI.

---

# Working With This Architecture (Agent Guide)

## General Rules
- Keep business rules in `Domain`.
- Add application behavior in `Application` as small, testable services/use-cases.
- Keep EF Core and external integrations in `Infrastructure`.
- UI (Razor Pages) calls Application services via DI.

## Common Tasks
- Update `Program.cs` only for DI, DbContexts, and minimal web configuration.
- Put migrations under the correct DbContext project (WebApp vs Products module).
- Write unit tests in `Tests/*` using Moq for interfaces.

---

# Adding a New Feature (Example: Create Product)

1) Domain
- Add entity changes if needed (e.g., validations).
- Add interface method to `Interfaces/IProductRepository` (e.g., `Task Add(ProductModel product)`).

2) Infrastructure
- Implement the new method in `ProductRepository`.
- Update `ProductDbContext` entity configuration if needed.
- Add EF Core migration in the Products module if schema changes:
  - Run from solution directory: `dotnet ef migrations add <Name> -p Modules/SD.ProjectName.Modules.Products -s Application/SD.ProjectName.WebApp -c ProductDbContext`
  - Update database: `dotnet ef database update -p Modules/SD.ProjectName.Modules.Products -s Application/SD.ProjectName.WebApp -c ProductDbContext`

3) Application
- Create a new use case (e.g., `CreateProduct`) that depends on `IProductRepository`.
- Keep logic small and testable; avoid EF specifics here.

4) Web UI (Razor Pages)
- Add page model (e.g., `Pages/Products/Create.cshtml.cs`) injecting `CreateProduct`.
- Add view `Create.cshtml` with form binding.

5) DI Registration (Program.cs)
- Ensure `IProductRepository` and new use case (`CreateProduct`) are registered.

6) Tests
- Add unit tests in `Tests/SD.ProjectName.Tests.Products`:
  - Mock `IProductRepository` to verify interactions.
  - Test success, validation, and edge cases.

---

# Conventions
- Application services: simple classes with clear method names (`GetList`, `Create`, `Update`).
- Avoid coupling Application to EF Core�use interfaces.
- Keep Razor Pages lean; delegate work to Application.
- One DbContext per bounded context (Identity vs Products).

# Performance & Maintainability
- Query shaping is in repository implementations.
- Use async everywhere (`Task`, `await`).
- Prefer small, composable services.

# Checklist When Modifying
- Domain API changes require Infrastructure updates and tests.
- Schema changes require migrations in the correct project and DbContext.
- Update DI registrations in `Program.cs`.
- Add/adjust Razor Pages if the UI changes.
- Ensure unit tests cover new paths.

# Useful Paths
- WebApp: `Application/SD.ProjectName.WebApp`.
- Products Module: `Modules/SD.ProjectName.Modules.Products`.
- Tests: `Tests/SD.ProjectName.Tests.Products`.

# Notes
- Target: .NET 9, C# 13.
- Prefer minimal changes that follow existing patterns.

---

# Proposed Mercato Marketplace Module Structure

This section outlines the proposed modular boundaries and responsibilities for the Mercato multi-vendor e-commerce marketplace. Each module follows the established layered architecture pattern (Domain → Application → Infrastructure) and maintains clear separation of concerns.

## Module Overview

Based on the Product Requirements Document (PRD, see `prd.md`) epics and marketplace requirements, the following modules are proposed:

| Module | Primary Responsibility | DbContext |
|--------|----------------------|-----------|
| Identity | Authentication, authorization, user management | ApplicationDbContext |
| Seller | Seller accounts, store management, onboarding | SellerDbContext |
| Buyer | Buyer profiles, preferences, purchase history | BuyerDbContext |
| Product | Product catalog, categories, attributes | ProductDbContext |
| Cart | Shopping cart, promotions, cart updates | CartDbContext |
| Orders | Order lifecycle, fulfillment, tracking | OrderDbContext |
| Payments | Payment processing, refunds, settlements | PaymentDbContext |
| Shipping | Shipping configuration, delivery tracking | ShippingDbContext |
| Reviews | Product reviews, seller ratings | ReviewDbContext |
| Notifications | Email, in-app notifications, messaging | NotificationDbContext |
| Admin | Platform administration, moderation, reporting | AdminDbContext |

---

## Module Responsibilities and Boundaries

### 1. Identity Module

**Path:** `Modules/SD.Mercato.Modules.Identity`

**Responsibility:**
- User authentication (login, logout, session management)
- User authorization and role-based access control (RBAC)
- Registration flows for buyers and sellers
- Social login integration (OAuth providers)
- Email verification and password reset
- Security token management

**Boundaries:**
- **Owns:** User credentials, authentication tokens, roles, claims
- **Exposes:** Authentication services, user identity claims, authorization policies
- **Does NOT handle:** User-specific profiles (delegated to Buyer/Seller modules), business logic for buyers or sellers

**Key Domain Entities:**
- `User`, `Role`, `UserRole`, `RefreshToken`, `EmailVerificationToken`

**Integration Points:**
- Consumed by all other modules for authentication/authorization
- Publishes domain events: `UserRegistered`, `UserLoggedIn`, `PasswordReset`

---

### 2. Seller Module

**Path:** `Modules/SD.Mercato.Modules.Seller`

**Responsibility:**
- Seller account management and onboarding workflow
- Store profile creation and verification
- Public store page configuration
- Payout settings and bank account management
- Seller dashboard and sales analytics
- Seller-specific settings and preferences

**Boundaries:**
- **Owns:** Seller profiles, store configurations, payout details, verification status
- **Exposes:** Seller information for product listings, store details for public pages
- **Does NOT handle:** Product management (Products module), order processing (Orders module), payment execution (Payments module)

**Key Domain Entities:**
- `Seller`, `Store`, `SellerVerification`, `PayoutSettings`, `BankAccount`

**Integration Points:**
- References Identity module for seller user accounts
- Products module references seller for product ownership
- Orders module references seller for order fulfillment
- Payments module references seller for commission calculations

---

### 3. Buyer Module

**Path:** `Modules/SD.Mercato.Modules.Buyer`

**Responsibility:**
- Buyer account and profile management
- Wish list management
- Purchase history tracking
- Shopping preferences and saved addresses
- Recently viewed items tracking

**Boundaries:**
- **Owns:** Buyer profiles, wish lists, saved addresses, preferences
- **Exposes:** Buyer information for checkout, shipping addresses
- **Does NOT handle:** Cart operations (Cart module), order creation (Orders module), payment processing (Payments module)

**Key Domain Entities:**
- `Buyer`, `WishList`, `WishListItem`, `SavedAddress`, `BuyerPreferences`

**Integration Points:**
- References Identity module for buyer user accounts
- Cart module references buyer for cart ownership
- Orders module references buyer for order history
- Reviews module references buyer for review authorship

---

### 4. Product Module

**Path:** `Modules/SD.Mercato.Modules.Products`

**Responsibility:**
- Product catalog management (CRUD operations)
- Category tree and hierarchy management
- Product attributes and specifications
- Product search and filtering
- Bulk product updates and CSV import/export
- Product workflow states (draft, published, archived)
- Inventory tracking

**Boundaries:**
- **Owns:** Products, categories, attributes, product images, inventory levels
- **Exposes:** Product catalog for search, product details for cart/orders
- **Does NOT handle:** Cart logic (Cart module), order processing (Orders module), seller management (Seller module)

**Key Domain Entities:**
- `Product`, `Category`, `ProductAttribute`, `ProductImage`, `Inventory`

**Integration Points:**
- References Seller module for product ownership
- Cart module references products for cart items
- Orders module references products for order line items
- Reviews module references products for product reviews

---

### 5. Cart Module

**Path:** `Modules/SD.Mercato.Modules.Cart`

**Responsibility:**
- Shopping cart management (add, update, remove items)
- Multi-seller cart support (items from multiple sellers)
- Cart totals calculation
- Promotion and discount application
- Cart persistence and recovery
- Cart-to-checkout transition

**Boundaries:**
- **Owns:** Cart state, cart items, applied promotions
- **Exposes:** Cart summary for checkout, cart item counts for UI
- **Does NOT handle:** Order creation (Orders module), payment processing (Payments module), inventory management (Products module)

**Key Domain Entities:**
- `Cart`, `CartItem`, `AppliedPromotion`, `CartSummary`

**Integration Points:**
- References Buyer module for cart ownership
- References Product module for product information and pricing
- Orders module consumes cart data to create orders
- May publish events: `CartUpdated`, `CartAbandoned`

---

### 6. Orders Module

**Path:** `Modules/SD.Mercato.Modules.Orders`

**Responsibility:**
- Order creation from cart checkout
- Order splitting by seller (multi-vendor support)
- Order status management and lifecycle
- Order details and line items
- Returns initiation and management
- Order history and tracking

**Boundaries:**
- **Owns:** Orders, order items, order statuses, return requests
- **Exposes:** Order information for tracking, order data for payments and shipping
- **Does NOT handle:** Payment execution (Payments module), shipping logistics (Shipping module), product catalog (Products module)

**Key Domain Entities:**
- `Order`, `OrderItem`, `OrderStatus`, `ReturnRequest`, `SellerOrder`

**Integration Points:**
- References Buyer module for order ownership
- References Seller module for seller-specific sub-orders
- References Product module for order line item details
- Consumes Cart module for checkout
- Triggers Payments module for payment processing
- Triggers Shipping module for shipment creation
- Publishes events: `OrderCreated`, `OrderStatusChanged`, `ReturnRequested`

---

### 7. Payments Module

**Path:** `Modules/SD.Mercato.Modules.Payments`

**Responsibility:**
- Payment provider integration (Stripe, PayPal, etc.)
- Payment processing and transaction management
- Escrow model implementation
- Seller payouts and commission calculations
- Refund processing
- Commission invoice generation
- Payment method management

**Boundaries:**
- **Owns:** Transactions, payment methods, payouts, commissions, refunds
- **Exposes:** Payment status for orders, transaction history
- **Does NOT handle:** Order management (Orders module), seller bank details storage (Seller module)

**Key Domain Entities:**
- `Transaction`, `PaymentMethod`, `Payout`, `Commission`, `Refund`, `EscrowHold`

**Integration Points:**
- Consumed by Orders module for order payment
- References Seller module for payout destination
- References Orders module for refund association
- Integrates with external payment providers
- Publishes events: `PaymentCompleted`, `RefundProcessed`, `PayoutExecuted`

---

### 8. Shipping Module

**Path:** `Modules/SD.Mercato.Modules.Shipping`

**Responsibility:**
- Shipping method configuration
- Shipping rate calculation
- Tracking number management
- Delivery status tracking
- Shipping label generation (Phase 2)
- Carrier integrations (Phase 2)
- CSV export for shipping manifests

**Boundaries:**
- **Owns:** Shipping methods, shipping rates, tracking information, shipments
- **Exposes:** Shipping options for checkout, tracking data for orders
- **Does NOT handle:** Order management (Orders module), seller configuration (Seller module)

**Key Domain Entities:**
- `Shipment`, `ShippingMethod`, `ShippingRate`, `TrackingEvent`, `Carrier`

**Integration Points:**
- Consumed by Orders module for shipment creation
- References Seller module for seller shipping configurations
- May integrate with external carrier APIs (Phase 2)
- Publishes events: `ShipmentCreated`, `TrackingUpdated`, `ShipmentDelivered`

---

### 9. Reviews Module

**Path:** `Modules/SD.Mercato.Modules.Reviews`

**Responsibility:**
- Product review submission and management
- Seller rating and feedback
- Review moderation workflow
- Rating aggregation and calculation
- Review response management

**Boundaries:**
- **Owns:** Reviews, ratings, review responses, moderation status
- **Exposes:** Aggregated ratings for products/sellers, review data for display
- **Does NOT handle:** Product management (Products module), seller management (Seller module), order verification (Orders module)

**Key Domain Entities:**
- `ProductReview`, `SellerRating`, `ReviewResponse`, `ModerationDecision`

**Integration Points:**
- References Buyer module for review authorship
- References Product module for product reviews
- References Seller module for seller ratings
- References Orders module to verify purchase before review
- Admin module consumes for moderation
- Publishes events: `ReviewSubmitted`, `ReviewModerated`

---

### 10. Notifications Module

**Path:** `Modules/SD.Mercato.Modules.Notifications`

**Responsibility:**
- Email notification delivery
- In-app notification management
- Notification preferences and subscriptions
- Notification templates and localization
- Messaging between buyers and sellers (Phase 1.5)
- Push notification support (future)

**Boundaries:**
- **Owns:** Notification templates, notification history, user preferences, messages
- **Exposes:** Notification sending interface for other modules
- **Does NOT handle:** Business logic triggering notifications (other modules publish events)

**Key Domain Entities:**
- `Notification`, `NotificationTemplate`, `NotificationPreference`, `Message`, `Conversation`

**Integration Points:**
- Subscribes to domain events from all modules
- References Identity module for user contact information
- Integrates with email service providers
- May integrate with push notification services

---

### 11. Admin Module

**Path:** `Modules/SD.Mercato.Modules.Admin`

**Responsibility:**
- Platform administration dashboard
- User management and moderation
- Seller verification and approval
- Content moderation (reviews, products)
- Platform configuration and settings
- KPI reporting and analytics
- Revenue reports and marketplace insights
- Audit logging and compliance

**Boundaries:**
- **Owns:** Admin settings, audit logs, moderation decisions, platform configuration
- **Exposes:** Administrative actions, reporting data, configuration settings
- **Does NOT handle:** Core business operations (delegated to respective modules)

**Key Domain Entities:**
- `AuditLog`, `ModerationAction`, `PlatformSetting`, `AdminReport`, `KpiSnapshot`

**Integration Points:**
- Has read access to all module data for reporting
- Can trigger moderation actions in Reviews module
- Can manage users in Identity module
- Can approve/suspend sellers in Seller module
- Consumes events for audit logging

---

## Inter-Module Communication

### Communication Patterns

1. **Synchronous (Direct Dependency Injection)**
   - Use for queries where immediate response is needed
   - Module exposes interfaces in `Domain/Interfaces`
   - Consuming module injects the interface via DI

2. **Asynchronous (Domain Events)**
   - Use for notifications and eventual consistency
   - Module publishes events when significant state changes occur
   - Other modules subscribe and react to events
   - Recommended for cross-cutting concerns like notifications and audit logging

### Example Event Flow

```
[Orders Module] --> OrderCreated event
    |
    ├── [Payments Module] --> Initiate payment
    ├── [Notifications Module] --> Send confirmation email
    └── [Admin Module] --> Log audit entry
```

---

## Proposed Folder Structure

```
src/
├── Application/
│   └── SD.Mercato.WebApp           # Razor Pages UI, DI, startup
├── Modules/
│   ├── SD.Mercato.Modules.Identity
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Seller
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Buyer
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Products
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Cart
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Orders
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Payments
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Shipping
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Reviews
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   ├── SD.Mercato.Modules.Notifications
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/
│   └── SD.Mercato.Modules.Admin
│       ├── Domain/
│       ├── Application/
│       └── Infrastructure/
├── Shared/
│   └── SD.Mercato.Shared.Kernel    # Shared abstractions, events, value objects
└── Tests/
    ├── SD.Mercato.Tests.Identity
    ├── SD.Mercato.Tests.Seller
    ├── SD.Mercato.Tests.Buyer
    ├── SD.Mercato.Tests.Products
    ├── SD.Mercato.Tests.Cart
    ├── SD.Mercato.Tests.Orders
    ├── SD.Mercato.Tests.Payments
    ├── SD.Mercato.Tests.Shipping
    ├── SD.Mercato.Tests.Reviews
    ├── SD.Mercato.Tests.Notifications
    └── SD.Mercato.Tests.Admin
```

---

## Module Dependency Rules

### Allowed Dependencies

```
UI (WebApp) → Application (any module) → Domain (same module)
Infrastructure (module) → Domain (same module)
Module A.Application → Module B.Domain (for cross-module queries)
```

### Prohibited Dependencies

```
Domain → Application (inverts dependency)
Domain → Infrastructure (inverts dependency)
Application → Infrastructure (should use interfaces)
Module A.Infrastructure → Module B.Infrastructure (tight coupling)
```

---

## Implementation Priorities (MVP)

Based on the PRD phases, recommended implementation order:

1. **Phase 1 (MVP Core):**
   - Identity (authentication foundation)
   - Seller (onboarding, store creation)
   - Buyer (basic profile)
   - Product (catalog management)
   - Cart (shopping cart)
   - Orders (order creation)
   - Payments (basic payment processing)

2. **Phase 1.5:**
   - Notifications (email + in-app)
   - Reviews (basic reviews)
   - Admin (basic administration)

3. **Phase 2:**
   - Shipping (carrier integrations)
   - Advanced Admin (reporting, analytics)
   - Enhanced Notifications (messaging)

---

## Security and Compliance Considerations

### Per-Module Security

| Module | Key Security Concerns |
|--------|----------------------|
| Identity | Password hashing, token security, session management, RBAC |
| Seller | PII protection, bank account data encryption, verification workflows |
| Buyer | GDPR data minimization, address encryption, consent management |
| Payments | PCI compliance, secure API keys, transaction logging |
| Admin | Audit logging, privileged access controls, RBAC enforcement |

### Cross-Cutting Concerns

- **Audit Logging:** All modules should publish events consumed by Admin module
- **Data Encryption:** PII and sensitive data encrypted at rest
- **RBAC:** Enforced via Identity module authorization policies
- **GDPR Compliance:** Data export/deletion capabilities per module
