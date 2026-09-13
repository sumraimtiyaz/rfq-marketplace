# B2B RFQ Marketplace

A working mini marketplace where **buyers publish requirements (RFQs)** and **suppliers discover
them and submit quotations**.

```
   BUYER                                          SUPPLIER
     │                                                │
     │ create RFQ ──────────►  ┌───────────┐          │
     │                         │    RFQ    │ ◄─── browse / search
     │                         └─────┬─────┘          │
     │                               │                │ submit quotation
     │                               ▼                │
     │                         ┌───────────┐ ◄────────┘
     └── view quotations ────► │ QUOTATION │
                               └───────────┘
```

---

## Contents

- [Live demo](#live-demo)
- [Features](#features)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Database schema](#database-schema)
- [Authentication and authorization](#authentication-and-authorization)
- [Validation](#validation)
- [API reference](#api-reference)
- [Running it](#running-it)
- [Environment variables](#environment-variables)
- [Tests](#tests)
- [CI/CD](#cicd)
- [Deployment](#deployment)
- [Assumptions](#assumptions)
- [Limitations](#limitations)

---

## Live demo

**App:** [frontend-sigma-six-18.vercel.app](https://frontend-sigma-six-18.vercel.app)
**API / Swagger:** [rfq-marketplace-api.onrender.com/swagger](https://rfq-marketplace-api.onrender.com/swagger)

Frontend on Vercel, API on Render, database on Neon — see [Deployment](#deployment) for how it's
wired together, or `docker compose up` for the whole stack in one command locally (see
[Running it](#running-it)).

---

## Features

### Buyer

- Register and sign in as a buying organisation
- Create, edit and delete RFQs (product, description, quantity, delivery location, deadline)
- Close an RFQ to stop accepting quotations, and reopen it later
- See every quotation received on an RFQ, ordered cheapest first, with the lowest price and
  fastest lead time marked
- Dashboard counters: total RFQs, open, closed, quotations received

### Supplier

- Register and sign in as a supplying organisation
- Browse all open RFQs with debounced full-text search, location filter, sorting and paging
- Open an RFQ to read the full requirement
- Submit exactly one quotation per RFQ (price, delivery days, optional message)
- Review every quotation they have submitted
- Dashboard counters: open requirements, awaiting your quote, quotations submitted

### Across the app

- Role-based access control enforced at the API, plus resource-level ownership checks
- Three-layer validation: Zod in the browser, FluentValidation in the API, constraints in
  the database
- Explicit loading, empty, error, unauthorized, forbidden and not-found states on every page
- Responsive from 320 px upwards
- OpenAPI/Swagger documentation
- 98 automated tests covering the flow, the rules and the boundaries

---

## Tech stack

| Layer          | Choice                                       |
| -------------- | -------------------------------------------- |
| Frontend       | Next.js 16 (App Router), React 19, TypeScript |
| Styling        | Tailwind CSS v4                              |
| Forms          | React Hook Form + Zod                        |
| Server state   | TanStack Query v5                            |
| Backend        | ASP.NET Core 8 Web API                       |
| ORM            | Entity Framework Core 8                      |
| Database       | PostgreSQL 16                                |
| Auth           | ASP.NET Core Identity + JWT in an HTTP-only cookie |
| Validation     | FluentValidation                             |
| Logging        | Serilog                                      |
| API docs       | Swashbuckle (OpenAPI)                        |
| Tests          | xUnit + WebApplicationFactory + FluentAssertions |
| Local env      | Docker Compose                               |
| CI             | GitHub Actions                               |

---

## Architecture

```
┌──────────────────────────────────────────────┐
│  Next.js (browser + Node server)             │
│                                              │
│  React components ── TanStack Query          │
│           │                                  │
│           │  fetch("/api/…")   same-origin   │
│           ▼                                  │
│  Next.js rewrite proxy  ─────────────────────┼──┐
└──────────────────────────────────────────────┘  │
                                                  │ HTTP
┌──────────────────────────────────────────────┐  │
│  ASP.NET Core Web API                        │ ◄┘
│                                              │
│  Controllers   ← [Authorize(Roles = …)]      │
│      │                                       │
│  Services      ← ownership checks,           │
│      │           business rules              │
│  EF Core DbContext                           │
└──────────────────┬───────────────────────────┘
                   │
           ┌───────▼────────┐
           │   PostgreSQL   │
           └────────────────┘
```

### Project layout

```
b2b-rfq-marketplace/
├── backend/
│   ├── RfqMarketplace.sln
│   ├── RfqMarketplace.Api/
│   │   ├── Controllers/        Auth, Rfqs, Quotations, Dashboard, Health
│   │   ├── Services/           business logic + ownership checks
│   │   ├── Models/             EF entities
│   │   ├── DTOs/               API contracts (never EF entities)
│   │   ├── Validators/         FluentValidation rules
│   │   ├── Data/               DbContext, migrations, seeder
│   │   ├── Middleware/         ProblemDetails exception handler
│   │   ├── Common/             JWT options, cookie helper, exception types
│   │   └── Dockerfile
│   └── RfqMarketplace.Api.Tests/
│       ├── Infrastructure/     WebApplicationFactory + helpers
│       ├── AuthenticationTests.cs
│       ├── AuthorizationTests.cs
│       ├── RfqValidationTests.cs
│       ├── QuotationTests.cs
│       └── MarketplaceFlowTests.cs
├── frontend/
│   ├── src/app/                routes (App Router)
│   ├── src/components/         ui / layout / rfq / quotation
│   ├── src/lib/                api client, auth, queries, validation, formatting
│   ├── src/types/              API response types
│   ├── src/proxy.ts           route guard
│   └── Dockerfile
├── .github/workflows/ci.yml
├── docker-compose.yml
└── .env.example
```

### Pages

```
/                       landing (signed-out only)
/login
/register

/buyer/rfqs             my RFQs + dashboard counters
/buyer/rfqs/new         create
/buyer/rfqs/[id]        detail + quotations received
/buyer/rfqs/[id]/edit   edit

/supplier/rfqs          browse + search + filter
/supplier/rfqs/[id]     detail + submit quotation
/supplier/quotations    my quotations
```

---

## Database schema

```
┌────────────────────┐
│ AspNetUsers        │
│────────────────────│
│ Id            uuid │──┐
│ Name          text │  │
│ CompanyName   text │  │
│ Email         text │  │  (+ Identity columns:
│ PasswordHash  text │  │   PasswordHash, security
│ CreatedAt     tstz │  │   stamps, lockout, …)
└────────────────────┘  │
         │              │
         │ 1            │ 1
         │              │
         │ N            │ N
┌────────▼───────────┐  │   ┌─────────────────────────┐
│ Rfqs               │  │   │ Quotations              │
│────────────────────│  │   │─────────────────────────│
│ Id            uuid │  └──►│ SupplierId         uuid │
│ BuyerId       uuid │      │ RfqId              uuid │
│ ProductName   text │◄─────│ QuotedPrice   numeric   │
│ Description   text │  1:N │ EstimatedDeliveryDays   │
│ Quantity       int │      │ Message            text │
│ DeliveryLocation   │      │ CreatedAt / UpdatedAt   │
│ Deadline      date │      └─────────────────────────┘
│ Status        text │        UNIQUE (RfqId, SupplierId)
│ CreatedAt / Updated│        CHECK  QuotedPrice > 0
└────────────────────┘        CHECK  EstimatedDeliveryDays > 0
  CHECK Quantity > 0
```

Relationships:

- one **buyer** has many **RFQs** (`Rfqs.BuyerId`)
- one **RFQ** receives many **quotations** (`Quotations.RfqId`)
- one **supplier** submits many **quotations** (`Quotations.SupplierId`)

`BuyerId` is what makes ownership checks possible: it is the column every mutation and every
quotation read is authorized against.

Roles live in Identity's own `AspNetRoles` / `AspNetUserRoles` tables rather than a `Role`
column, so `[Authorize(Roles = "Buyer")]` and the application agree on one source of truth.

---

## Authentication and authorization

### How sign-in works

```
POST /api/auth/login
        │
        ▼
ASP.NET Core Identity verifies the password hash
        │
        ▼
JWT signed (HS256) with the user id, email and role
        │
        ├──► Set-Cookie: rfq_token=…; HttpOnly; Secure; SameSite=Lax; Path=/
        └──► also in the response body, for Swagger/Postman/tests
```

### Permission matrix

| Action                            | Buyer            | Supplier |
| --------------------------------- | ---------------- | -------- |
| Create RFQ                        | ✅               | ❌ 403   |
| Edit / delete / close own RFQ     | ✅               | ❌ 403   |
| Edit another buyer's RFQ          | ❌ 403           | ❌ 403   |
| Browse the marketplace            | ❌ 403           | ✅       |
| View RFQ detail                   | own only         | any RFQ  |
| View quotations on an RFQ         | own RFQs only    | ❌ 403   |
| Submit a quotation                | ❌ 403           | ✅       |
| View own quotations               | ❌ 403           | ✅       |
| View another supplier's quotation | ❌               | ❌       |


---

## Validation

Three layers, each with a different job:

| Layer                          | Purpose                                        |
| ------------------------------ | ---------------------------------------------- |
| Zod (browser)                  | immediate feedback while typing                |
| FluentValidation (API)         | **the security boundary** — re-checks everything |
| PostgreSQL CHECK / UNIQUE      | last resort against bugs and races             |

Anything the browser checks can be skipped by a client that chooses to, which is why the API
re-validates every field on every request.

### Rules

**RFQ** — product name required, ≤ 200 chars · description required, ≤ 5000 · quantity an integer
> 0 and ≤ 10,000,000 · delivery location required, ≤ 200 · deadline today or later, within 5 years.

**Quotation** — quoted price > 0, ≤ 999,999,999,999.99, at most 2 decimal places · estimated
delivery 1–3650 days · message optional, ≤ 2000 chars.

**Account** — name and company required · valid, unique email · password ≥ 8 chars with an
upper case letter, a lower case letter and a digit · role must be `Buyer` or `Supplier`.

Failures come back as RFC 7807 `ValidationProblemDetails`, and the frontend attaches each message
to the matching input:

```json
{
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "errors": {
    "Quantity": ["Quantity must be greater than 0."],
    "Deadline": ["Deadline must be today or a future date."]
  },
  "traceId": "0HNOEHUFBCS44:00000001"
}
```

### Business rules

- A quotation is rejected if the RFQ is **closed** or **past its deadline** (`400`).
- A supplier may submit **one quotation per RFQ** (`409`), enforced by a unique index.
- An RFQ can only be **edited while open**; closing it freezes the terms suppliers quoted against.
- Deleting an RFQ cascades to its quotations.

---

## API reference

Interactive docs at **`/swagger`** when `Swagger:Enabled` is true (default in Development and in
the compose stack).

### Auth

| Method | Path                 | Access        | Notes                                  |
| ------ | -------------------- | ------------- | -------------------------------------- |
| POST   | `/api/auth/register` | anonymous     | `201`, sets the auth cookie            |
| POST   | `/api/auth/login`    | anonymous     | `200`, sets the auth cookie            |
| POST   | `/api/auth/logout`   | anonymous     | `204`, clears the cookie               |
| GET    | `/api/auth/me`       | authenticated | current user and role                  |

### RFQs

| Method | Path                        | Access            | Notes                                     |
| ------ | --------------------------- | ----------------- | ----------------------------------------- |
| POST   | `/api/rfqs`                 | Buyer             | create                                    |
| GET    | `/api/rfqs`                 | Supplier          | browse; see query params below            |
| GET    | `/api/rfqs/my`              | Buyer             | own RFQs, paged                           |
| GET    | `/api/rfqs/locations`       | Supplier          | distinct locations, for the filter        |
| GET    | `/api/rfqs/{id}`            | authenticated     | buyers: own only; suppliers: any          |
| PUT    | `/api/rfqs/{id}`            | Buyer (owner)     | only while open                           |
| DELETE | `/api/rfqs/{id}`            | Buyer (owner)     | cascades to quotations                    |
| POST   | `/api/rfqs/{id}/close`      | Buyer (owner)     | stop accepting quotations                 |
| POST   | `/api/rfqs/{id}/reopen`     | Buyer (owner)     | put it back on the market                 |
| GET    | `/api/rfqs/{id}/quotations` | Buyer (owner)     | cheapest first                            |
| POST   | `/api/rfqs/{id}/quotations` | Supplier          | one per supplier per RFQ                  |

Browse query parameters: `search`, `location`, `status`, `includeExpired`, `sortBy`
(`createdAt` · `deadline_asc` · `quantity_desc`), `page`, `pageSize` (max 50).

### Quotations, dashboard, health

| Method | Path                       | Access    |
| ------ | -------------------------- | --------- |
| GET    | `/api/quotations/my`       | Supplier  |
| GET    | `/api/dashboard/buyer`     | Buyer     |
| GET    | `/api/dashboard/supplier`  | Supplier  |
| GET    | `/api/health`              | anonymous |

### Status codes

| Code  | Meaning                                                   |
| ----- | --------------------------------------------------------- |
| `400` | validation failed, or a business rule rejected the request |
| `401` | not signed in, or the token is invalid/expired             |
| `403` | wrong role, or the resource belongs to someone else        |
| `404` | no such RFQ                                                |
| `409` | duplicate email, or a second quotation on the same RFQ     |
| `500` | unexpected — logged in full, generic message to the client |

---

## Running it

### Option A — Docker Compose (everything, one command)

```bash
cp .env.example .env
```

Set `JWT_KEY` in `.env` to something at least 32 characters long — compose refuses to start
without it:

```bash
openssl rand -base64 48
```

Then:

```bash
docker compose up --build
```

- Web app → <http://localhost:3000>
- API + Swagger → <http://localhost:5080/swagger>

Migrations run and the demo data seeds automatically on first start.

> **Port already in use?** Change `POSTGRES_PORT`, `API_PORT` or `FRONTEND_PORT` in `.env`.
> Those are host ports only; the containers always reach each other on their internal ports.
> Postgres is published on **5434** by default rather than 5432, because a locally installed
> PostgreSQL usually holds 5432 already.

### Option B — run the apps directly (better for development)

**Prerequisites:** .NET 8 SDK, Node.js 20+, and a PostgreSQL you can reach.

**1. Database.** Either use an existing PostgreSQL, or start one:

```bash
docker run -d --name rfq-postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=rfq_marketplace -p 5434:5432 postgres:16-alpine
```

**2. API.** `appsettings.Development.json` already carries a development signing key, so no
setup is needed beyond a reachable database:

```bash
cd backend/RfqMarketplace.Api
dotnet run
```

Listens on <http://localhost:5080>, applies migrations, seeds the demo data and serves Swagger
at `/swagger`. If your database is not on `localhost:5434`, override it:

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=rfq_marketplace;Username=postgres;Password=postgres" dotnet run
```

**3. Frontend.**

```bash
cd frontend
npm install
npm run dev
```

Opens on <http://localhost:3000> and proxies `/api/*` to the API. Point it elsewhere by setting
`BACKEND_URL` in `frontend/.env.local`.

### Migrations

```bash
dotnet tool install --global dotnet-ef
cd backend

dotnet ef migrations add <Name> --project RfqMarketplace.Api --startup-project RfqMarketplace.Api --output-dir Data/Migrations
dotnet ef database update --project RfqMarketplace.Api --startup-project RfqMarketplace.Api
```

They also run automatically at startup (`Database:MigrateOnStartup`, default `true`), which is
what makes a fresh container immediately usable. For a production rollout with more than one
replica you would turn that off and run migrations as a separate deployment step.

---

## Environment variables

### API

| Variable                               | Default                     | Purpose                                    |
| -------------------------------------- | --------------------------- | ------------------------------------------ |
| `ConnectionStrings__DefaultConnection`  | localhost:5434              | PostgreSQL connection string               |
| `JWT__KEY`                             | *(required in Production)*  | HS256 signing key, **≥ 32 characters**     |
| `JWT__ISSUER` / `JWT__AUDIENCE`        | `RfqMarketplace…`           | token issuer and audience                  |
| `JWT__EXPIRYMINUTES`                   | `480`                       | token lifetime                             |
| `JWT__COOKIESECURE`                    | `true`                      | set `false` only for plain-HTTP localhost  |
| `JWT__COOKIESAMESITE`                  | `Lax`                       | `None` (+ Secure) if the API is cross-site |
| `CORS__ALLOWEDORIGINS__0`              | `http://localhost:3000`     | allowed origin for credentialed requests   |
| `DATABASE__MIGRATEONSTARTUP`           | `true`                      | apply migrations at boot                   |
| `DATABASE__SEEDDEMODATA`               | `true`                      | load demo accounts and RFQs when empty     |
| `SWAGGER__ENABLED`                     | `false`                     | serve `/swagger` outside Development       |

The app will not start in any environment without a `JWT__KEY` of at least 32 characters —
`ValidateOnStart` fails fast rather than issuing tokens signed with a weak or empty key.

### Frontend

| Variable      | Default                 | Purpose                            |
| ------------- | ----------------------- | ---------------------------------- |
| `BACKEND_URL` | `http://localhost:5080` | where `/api/*` is proxied          |

`next build` serialises the rewrite destination into its routes manifest, so `BACKEND_URL` has to
be set **at build time as well as at runtime** — changing it means rebuilding, not just restarting.
The Dockerfile takes it as a build arg and compose passes the same value to both.

### Compose

See [`.env.example`](.env.example) — `JWT_KEY`, `POSTGRES_*`, and the three host port overrides.

---

## Tests

98 tests, all integration tests through the real HTTP pipeline
(`WebApplicationFactory`): real routing, real authentication, real authorization, real validators.

```bash
cd backend
dotnet test
```

They run against **SQLite in memory** by default, so no Docker is needed. Point them at real
PostgreSQL — which is what CI does — with:

```bash
TEST_POSTGRES_CONNECTION="Host=localhost;Port=5434;Database=rfq_tests;Username=postgres;Password=postgres" dotnet test
```

Both configurations pass. Running against the real engine is what catches provider-specific
drift; an earlier version of the search query used `ILIKE`, which PostgreSQL has and SQLite
does not.

| Suite                     | Covers                                                                 |
| ------------------------- | ---------------------------------------------------------------------- |
| `AuthenticationTests`     | registration, login, cookie flags, cookie-only auth, tampered tokens, account enumeration, password hashing |
| `AuthorizationTests`      | every role boundary and every ownership boundary in the matrix above    |
| `RfqValidationTests`      | each field rule, all errors reported together, input trimming           |
| `QuotationTests`          | submission, duplicates, concurrent duplicates, closed/expired RFQs, price ordering, cascade delete |
| `MarketplaceFlowTests`    | the end-to-end buyer→supplier→buyer loop, search, filters, paging, dashboards |
| `ConfigurationTests`      | optional settings (e.g. `Swagger:Enabled`) fall back safely when blank, missing, or unparseable, instead of crashing the app at startup |

---

## CI/CD

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every push and PR to `main`:

```
┌── backend ────────────────────────┐   ┌── frontend ──────────┐
│ restore → build → test            │   │ install → lint       │
│ (against a PostgreSQL service)    │   │ → typecheck → build  │
└──────────────┬────────────────────┘   └──────────┬───────────┘
               └──────────────┬────────────────────┘
                              ▼
                   ┌── docker ──────────────┐
                   │ build both images      │
                   │ (layer cache in GHA)   │
                   └────────────────────────┘
```

The two lanes run in parallel; images only build once both are green.

---

## Deployment

Live now — see [Live demo](#live-demo) for the URLs. The deployed shape:

```
Next.js  ──►  Vercel        (frontend-sigma-six-18.vercel.app)
ASP.NET  ──►  Render         (rfq-marketplace-api.onrender.com/swagger, built from backend/RfqMarketplace.Api/Dockerfile)
Postgres ──►  Neon
```

Each was connected to this GitHub repository and deploys on push to `main`. `BACKEND_URL` on
Vercel points at the Render URL; `ConnectionStrings__DefaultConnection` on Render points at Neon.

---

## Assumptions

These were left open by the brief; this is how they were resolved.

1. **RFQ visibility** — every open RFQ is visible to every authenticated supplier. There is no
   invitation or private-RFQ concept.
2. **Quotation visibility** — only the buyer who owns the RFQ can see its quotations. A supplier
   sees only their own, and never learns how many others responded.
3. **One quotation per supplier per RFQ**, enforced at the database level. Revisions are not
   supported.
4. **Deadline** — the deadline day is inclusive: quotations are accepted until the end of that
   date (UTC), and rejected afterwards.
5. **Editing** — a buyer may edit an RFQ while it is open. Closing freezes it, so suppliers who
   already quoted are not quoted against terms that changed underneath them.
6. **Closing** — manual, and reversible. RFQs are not auto-closed at their deadline; they are
   shown as expired instead, which keeps the buyer's record of what happened intact.
7. **Deleting** — a buyer can delete their own RFQ at any time, and its quotations go with it.
8. **Company name** is collected at registration and is required. The brief's user table only had
   a personal name, but this is a B2B marketplace and "Supplier: ABC Furniture" is the
   identity that matters on both sides.
9. **Currency** — all prices are INR. There is no currency field.
10. **Roles are fixed at registration.** There is no role switching and no admin role.

---

## Limitations

Deliberately out of scope for an MVP:

- **No accept/reject.** Buyers can view and compare quotations but not award one.
- **Suppliers cannot edit or withdraw a quotation** once submitted.
- **No notifications** of any kind — no email, no in-app, no real-time updates. A buyer sees new
  quotations by loading the page.
- **No payments, contracts, invoicing or shipment tracking.**
- **No file attachments** (drawings, specifications, certificates).
- **No admin panel**, no user management, no moderation.
- **No refresh tokens.** The JWT lasts 8 hours and then the user signs in again. Adding rotation
  is the natural next step, and needs a token store to do properly.
- **No rate limiting** on authentication endpoints. In production this belongs at the edge or via
  ASP.NET Core's rate limiter.
- **Search is `LIKE`-based.** Fine at this size; a real catalogue would want PostgreSQL full-text
  search or a dedicated index.
- **No email verification or password reset.**
- **Single currency, single language.**
