# dmp.api.web

Main HTTP API of the DMP (Filezon) digital goods marketplace. It serves the buyer storefront
([dmp.client](https://github.com/denis-susha/dmp.client)) and the seller cabinet
([dmp.seller](https://github.com/denis-susha/dmp.seller)): catalog and full-text search, product management and
moderation, cart and orders, crypto payments through Bitcart, seller finances/withdrawals, support tickets,
registration/authentication, and a SignalR hub that pushes payment status updates to the browser. Background work
(sending mail, processing invoices and transactions) is done by the `dmp.job.*` services, which share the same
PostgreSQL databases and Redis instance.

## Tech stack

| Component | Version |
|---|---|
| .NET / ASP.NET Core (controllers, SignalR) | 10.0 |
| Entity Framework Core + Npgsql provider | 10.0 (Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3) |
| PostgreSQL | two databases: marketplace (`DmpConnection`) and billing (`BillingDbConnection`) |
| Redis Stack (RedisJSON + RediSearch) | StackExchange.Redis 3.3.1, NRedisStack 1.8.0 |
| MinIO / S3 object storage | Minio SDK 7.0.0 |
| Image processing | SixLabors.ImageSharp 3.1.12 |
| JWT authentication | Microsoft.AspNetCore.Authentication.JwtBearer 10.0, System.IdentityModel.Tokens.Jwt 8.x |
| OpenAPI | Microsoft.AspNetCore.OpenApi 10.0 + Scalar.AspNetCore 2.x (Development/Local only) |
| Misc | System.Linq.Dynamic.Core, Ulid, Bogus (test data generator endpoint) |

## Project structure

```
dmp.api.web/
├── API.Web/                 ASP.NET Core host: Program.cs, DI wiring, controllers, SignalR hub, filters
│   ├── Controllers/         REST endpoints (routes are lower-cased, e.g. /productmanager/...)
│   ├── Hubs/PaymentHub.cs   SignalR hub at /paymenthub ("ReceiveMessage" payment updates)
│   ├── Attributes/          Basic-auth filter for workers, locale extraction, OpenAPI hiding
│   └── OpenApi/             OpenAPI document transformers
├── DMP.BL/                  Business logic: services, DTOs, Redis/MinIO/Bitcart integration, helpers
├── DMP.DataAccess/          EF Core DbContexts (DmpDbContext, BillingDbContext) and entities
├── DMP.Crosscutting/        Small shared types (Redis settings, locale conversion)
├── email_templates/         Razor e-mail templates (en/ru); stored in the `email_templates` DB table and
│                            rendered by dmp.job.server - this API only enqueues mails with a JSON model
├── Directory.Build.props    Common MSBuild settings (net10.0, nullable, implicit usings)
├── Directory.Packages.props Central package versions
└── dmp.api.web.slnx         Solution
```

### Main route groups

| Prefix | Auth | Purpose |
|---|---|---|
| `/auth` | JWT | Login, token refresh, logout (tokens are set as HttpOnly cookies) |
| `/registration` | anonymous | Sign-up, e-mail confirmation, password reset (Cloudflare Turnstile protected) |
| `/user` | JWT | Profile and settings |
| `/catalog`, `/product`, `/page`, `/blog` | anonymous | Menus, category listings, product pages, search, localized page JSON |
| `/cart`, `/order`, `/download`, `/support` | JWT | Cart, checkout/payment (Bitcart invoices), purchased file downloads, tickets |
| `/productmanager`, `/store`, `/sales`, `/finances`, `/withdrawal`, `/seller` | JWT + `isSeller` claim | Seller cabinet |
| `/admin`, `/cache` | JWT + `admin` role | Moderation, bonuses/payouts, cache rebuild, test data generation |
| `/messages/workernotification` | HTTP Basic (`BasicAuthorize`) | Called by dmp.job.invoiceworker to push payment updates to SignalR clients |
| `/paymenthub` | JWT (cookie) | SignalR hub |

JSON uses camelCase with enums as strings. The JWT is read from the `access_token` cookie.

## Configuration

Settings are read from `appsettings.json`, `appsettings.{Environment}.json`, user secrets and environment variables
(`Section__Key`, e.g. `JwtSettings__SecretKey`). Committed files contain **no real secrets** - values marked
`change-me` or empty must be supplied for every environment.

| Setting | Description |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` (docker dev), `Local` (running on the host), `Production`. OpenAPI UI is enabled for Development/Local; auth cookies are `Secure` only in Production and have no domain in Local (otherwise `.filezon.com`). |
| `ASPNETCORE_HTTP_PORTS` | HTTP port inside the container (compose uses `80`; `appsettings.Development.json` also binds `http://*:80`). |
| `REDIS_HOST`, `REDIS_PORT`, `REDIS_PASSWORD` | Redis Stack connection (environment variables, shared with the other DMP services). |
| `Redis__enabled` | Enables the Redis cache layer (`true` by default). |
| `ConnectionStrings__DmpConnection` | Npgsql connection string of the marketplace database. |
| `ConnectionStrings__BillingDbConnection` | Npgsql connection string of the billing database. |
| `JwtSettings__SecretKey` | HMAC-SHA256 key for access tokens (at least 32 characters). Must match other services validating these tokens. |
| `JwtSettings__Issuer`, `JwtSettings__Audience` | Token issuer / audience (`your_issuer` / `api.auth`). |
| `JwtSettings__AccessTokenExpirationMinutes`, `JwtSettings__RefreshTokenExpirationDays` | Token lifetimes (15 / 15). |
| `JwtRegistrationSettings__SecretKey` | HMAC key for e-mail confirmation / password reset tokens. |
| `JwtRegistrationSettings__Issuer`, `__Audience`, `__TokenExpirationMinutes` | Registration token parameters (`your_issuer` / `api.reg` / 60). |
| `BitcartBackend__Url` | Base URL of the Bitcart merchant API (e.g. `http://bitcart-backend:8000/`). |
| `BasicAuthorize__User`, `BasicAuthorize__Password` | Credentials the worker services use for `/messages/workernotification`. |
| `Minio__endpoint` | MinIO endpoint as `host:port` (internal address). |
| `Minio__accessKey`, `Minio__secretKey` | Admin MinIO credentials (product images). |
| `Minio__s3PublicEndpoint` | Public base URL of the object storage used in links returned to browsers. |
| `Minio__SellerSettings__accessKey`, `__secretKey` | MinIO user used to generate presigned upload URLs for sellers. |
| `Minio__ClientSettings__accessKey`, `__secretKey` | MinIO user used to generate presigned download URLs for buyers. |
| `DmpHosts__Client`, `DmpHosts__Seller` | Public URLs of the storefront and seller cabinet (used in e-mail links). |
| `CfSettings__TURNSTILE_SECRET_KEY` | Cloudflare Turnstile secret (the `1x000...AA` value is Cloudflare's public test key). |
| `Cors__AllowedOrigins` | Array of allowed browser origins (credentials are allowed). |
| `AllowedHosts` | Host filtering list. |

## Getting started

### Prerequisites

- .NET SDK 10.0 (see `global.json`)
- PostgreSQL with the `dmarketplace` and `billing` databases, Redis Stack, MinIO and a Bitcart instance.
  The easiest way to get all of them is the compose setup in [dmp.docker](https://github.com/denis-susha/dmp.docker).

### Run locally

```bash
# point the API at your local infrastructure (ports in appsettings.Local.json: Postgres 5442/5452, MinIO 9000)
dotnet user-secrets set "JwtSettings:SecretKey" "<random 32+ chars>" --project API.Web
dotnet user-secrets set "ConnectionStrings:DmpConnection" "Host=localhost;Port=5442;Database=dmarketplace;Username=admin;Password=<pwd>" --project API.Web
# ...other secrets as listed above
dotnet run --project API.Web --launch-profile http
```

The `http` launch profile uses the `Local` environment and listens on `http://localhost:5005`; set `REDIS_PASSWORD`
in `API.Web/Properties/launchSettings.json` or your shell. The API reference is served at `/scalar/v1`
(document at `/openapi/v1.json`) in Development/Local.

### Run with Docker

```bash
docker build -t dmp-apiweb .
cp .env.example .env   # fill in real values
docker run --rm -p 9090:80 --env-file .env dmp-apiweb
```

`.env.example` lists every variable the container needs.

In the full system the image is built and started by `dmp.docker` (service `api.web-dmp`, container `dmp-api-web`).

## Commands

| Command | Description |
|---|---|
| `dotnet build -c Release` | Build the solution |
| `dotnet format` | Apply `.editorconfig` code style |
| `dotnet run --project API.Web` | Run the API |
| `docker build -t dmp-apiweb .` | Build the container image |

## Related repositories

- [dmp](https://github.com/denis-susha/dmp) - umbrella repository with the system overview
- [dmp.client](https://github.com/denis-susha/dmp.client) - buyer storefront (Next.js), consumes this API and `/paymenthub`
- [dmp.seller](https://github.com/denis-susha/dmp.seller) - seller cabinet, consumes this API
- [dmp.job.server](https://github.com/denis-susha/dmp.job.server) - Hangfire jobs: renders and sends queued e-mails, product cache refresh, consumes the `sys_notify_queue` Redis list
- [dmp.job.invoiceworker](https://github.com/denis-susha/dmp.job.invoiceworker) - Bitcart invoice processing, calls `/messages/workernotification`
- [dmp.job.trxworker](https://github.com/denis-susha/dmp.job.trxworker) - billing transaction processing
- [dmp.api.notifications](https://github.com/denis-susha/dmp.api.notifications) - notifications API
- [dmp.docker](https://github.com/denis-susha/dmp.docker) - Docker Compose infrastructure (PostgreSQL, Redis, MinIO, Bitcart, nginx)
