# StoreAPI — E-commerce REST API

A REST API for an online food store, built with **ASP.NET Core 8 Web API** as a **training project**.
It covers the back-end flow of a shop: catalog browsing with filtering and paging, a Redis-backed
basket, JWT authentication, orders, and Stripe payments with webhook handling.

`C#` · `ASP.NET Core 8 Web API` · `EF Core` · `SQL Server` · `ASP.NET Core Identity` · `JWT` · `Redis` · `Stripe` · `AutoMapper` · `Swagger`

---

## Architecture

The solution is split into four projects with one-way dependencies:

```
Store.Web         → API controllers, middleware, DI setup, Swagger
Store.service     → business services (products, basket, orders, payments, tokens, caching), DTOs, AutoMapper profiles
Store.Repository  → generic repository, Unit of Work, Specification pattern, Redis basket repository, seeding
Store.Data        → entities, DbContexts, EF Core configurations and migrations
```

Main techniques used:

- **Specification pattern** for filtering, sorting, searching and paging products, and for order queries
- **Generic repository + Unit of Work** on top of EF Core
- **Two databases**: one for store data, one for ASP.NET Core Identity
- **Redis** for the customer basket and for response caching (a `[Cache(seconds)]` action filter)
- **Global exception-handling middleware** with a consistent error response
- **Stripe** payment intents, and a webhook that marks orders as paid or failed
- Migrations and seed data (brands, types, products, delivery methods, a test user) applied on startup

---

## Endpoints

All routes follow `api/{controller}/{action}`.

| Area | Method | Endpoint | Notes |
|---|---|---|---|
| Account | POST | `/api/Account/Login` | returns a JWT |
| Account | POST | `/api/Account/Register` | |
| Account | GET | `/api/Account/GetCurrentUserDetails` | JWT required |
| Products | GET | `/api/Product/GetAllProducts` | JWT required · query: `BrandId`, `TypeId`, `Sort`, `Search`, `PageIndex`, `PageSize` · cached |
| Products | GET | `/api/Product/GetProductById?id=` | JWT required |
| Products | GET | `/api/Product/GetAllBrands` · `/api/Product/GetAllTypes` | JWT required |
| Basket | GET / POST / DELETE | `/api/Basket/GetBasketAsync/{id}` · `UpdateBasketAsync` · `DeleteBasketAsync/{id}` | stored in Redis |
| Orders | POST | `/api/Order/CreateOrderAsync` | uses the user's email claim |
| Orders | GET | `/api/Order/GetAllOrderForUserAsync` · `GetOrderByIdAsync?id=` | uses the user's email claim |
| Orders | GET | `/api/Order/GetAllDeliveryMethodsAsync` | |
| Payment | POST | `/api/Payment/CreateOrUpdatePaymentIntent` | creates or updates a Stripe payment intent for a basket |
| Payment | POST | `/api/Payment/webhook` | Stripe webhook (`payment_intent.succeeded` / `payment_failed`) |

The full contract is available in Swagger when running in Development.

---

## Running locally

**Prerequisites:** .NET 8 SDK, SQL Server (or LocalDB), Redis, and a Stripe account in test mode.

```bash
git clone https://github.com/mohamed-wael15/StoreAPI.git
cd StoreAPI
```

1. **Connection strings** — `Store.Web/appsettings.json` points to a local SQL Server instance (Windows authentication)
   and to Redis on `localhost`. Adjust them if your setup is different.
2. **Secrets** — the JWT key and the Stripe keys (including the webhook signing secret) are intentionally empty in `appsettings.json`.
   Set them with user secrets (never commit them):

   ```bash
   dotnet user-secrets init --project Store.Web
   dotnet user-secrets set "Token:Key" "<random string, at least 32 characters>" --project Store.Web
   dotnet user-secrets set "Stripe:Secretkey" "<your sk_test_ key>" --project Store.Web
   dotnet user-secrets set "Stripe:publishableKey" "<your pk_test_ key>" --project Store.Web
   dotnet user-secrets set "Stripe:WebhookSecret" "<your whsec_ signing secret>" --project Store.Web
   ```

   For local webhook testing, `stripe listen --forward-to <your URL>/api/Payment/webhook` prints the `whsec_` secret to use.

   Environment variables work as well, e.g. `Token__Key` and `Stripe__Secretkey`.
3. **Run** — databases are migrated and seeded automatically on startup:

   ```bash
   dotnet run --project Store.Web
   ```

   Then open `/swagger` on the URL shown in the console.

---

## Notes

- This is a learning project: there is no front end and no automated tests.
- Product endpoints require a logged-in user, so register and log in first, then send the token as `Bearer <token>`.
