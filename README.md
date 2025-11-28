# Mercato

## Project Overview
Mercato is a multi-vendor e-commerce marketplace designed to connect independent online stores with buyers. The platform features distinct interfaces for buyers, sellers, and administrators. Its primary goals are to enable multi-store selling, facilitate seamless purchase flows, support seller operations, generate marketplace revenue via commissions, and provide the foundation for future integrations.

## System Architecture Summary
This solution uses a modular, layered architecture with a Razor Pages Web App and a feature module for Products.

**Projects:**
- `Application/SD.ProjectName.WebApp`: Razor Pages UI, app startup, DI, EF Core Identity.
- `Modules/SD.ProjectName.Modules.Products`: Feature module containing `Domain`, `Application`, `Infrastructure` for Products.
- `Tests/SD.ProjectName.Tests.Products`: Unit tests for module/application logic.

**Module Structure:**
- `Domain`: Core entities and repository interfaces (e.g., `ProductModel`, `IProductRepository`).
- `Application`: Use cases (application services) depending on `Domain` interfaces (e.g., `GetProducts`).
- `Infrastructure`: Implementations and persistence for the module (e.g., `ProductDbContext`, `ProductRepository`).

**Dependency Flow:**
- WebApp → Application (use cases) → Domain (interfaces)
- Infrastructure implements Domain interfaces and is registered in DI at startup.
- Tests reference Application and Domain, mocking `IProductRepository`.

## Key Modules and Features (from PRD)
**Epics in Scope:**
- **Identity & Access Management:** Registration and login (buyers/sellers), social login (buyers via Google and Facebook), email verification, password reset, session management.
- **Seller Management:** Seller onboarding, store profile/verification, public store page, payout settings.
- **Product Catalog Management:** Product CRUD, category tree, bulk updates, CSV import/export, product workflow.
- **Product Search & Navigation:** Search, filters, sorting, category pages, recently viewed items.
- **Shopping Cart & Checkout:** Multi-seller cart, shipping/payment integration, order confirmation.
- **Orders & Fulfilment:** Seller-split orders, order statuses, lists/details, returns initiation.
- **Payments & Settlements:** Payment provider integration, escrow, payouts, commission invoices, refunds.
- **Shipping & Delivery:** Shipping config, tracking numbers, CSV export. Integrations are deferred to Phase 2.
- **Returns & Disputes:** Return requests, seller review, messaging, admin escalation.
- **Reviews & Ratings:** Product reviews, seller ratings, admin moderation.
- **Notifications:** Email/in-app notifications. Messaging phase planned for 1.5.
- **Reporting:** Admin KPIs, seller dashboards, revenue reports.
- **Administration:** User management, moderation, platform settings.
- **Integrations & APIs:** Payments/shipping; public/private APIs & webhooks planned for Phase 2.
- **Security & Compliance:** GDPR, RBAC, audit logging.
- **UX & Mobile:** Responsive UI, design system, PWA support.

## Technology Stack and Design Decisions
- **Target:** .NET 9, C# 13
- **Web Framework:** ASP.NET Core Razor Pages
- **Data Access:** Entity Framework Core
- **Database:** Separate `DbContext` per bounded context (Identity vs Products)
- **Architecture:** Modular, layered architecture with CQRS-style separation
- **Testing:** Unit tests using mocked interfaces

**Conventions:**
- Application services: simple classes with clear method names (`GetList`, `Create`, `Update`).
- Avoid coupling Application to EF Core—use interfaces.
- Keep Razor Pages lean; delegate work to Application.
- One `DbContext` per bounded context.

## Quick Start Instructions
1. **Prerequisites:** .NET 9 SDK
2. **Clone the repository:**
   ```bash
   git clone https://github.com/SebastianDabkowski/mercato-1.git
   cd mercato-1
   ```
3. **Build the solution:**
   ```bash
   cd src
   dotnet build Mercato.sln
   ```
4. **Run the web application:**
   ```bash
   cd src/Mercato.Web
   dotnet run
   ```
5. **Run tests:**
   ```bash
   cd src
   dotnet test Mercato.sln
   ```

## Folder Structure Explanation
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

## Future Development Roadmap (from PRD)
- **MVP (Phase 1):** Login, onboarding, catalog, search, cart, checkout, payments, basic returns, admin basics.
- **Deferred/Post-MVP:** Advanced analytics, shipping integrations, product variants, promo codes, public API, extended messaging, integrations.
- **Phase 1.5:** Messaging in notifications.
- **Phase 2:** Shipping integrations, APIs, webhooks.

## Non-Functional Requirements
- Performance, scalability, security, reliability, GDPR compliance.

## Risks
- Payment provider approval processes, seller verification complexity, regulatory constraints.

## Notes for Contributors
- All contributions should align with the PRD-documented scope and epics.
- Respect clearly defined user role boundaries (Buyer, Seller, Admin).
- Uphold GDPR requirements (data minimalism, audit logging, RBAC).
- Do not introduce features marked out of scope for MVP or pre-allocated for later roadmap phases.
- Keep business rules in `Domain`.
- Add application behavior in `Application` as small, testable services/use-cases.
- Keep EF Core and external integrations in `Infrastructure`.
- UI (Razor Pages) calls Application services via DI.
- Write unit tests in `Tests/*` using mocks for interfaces.