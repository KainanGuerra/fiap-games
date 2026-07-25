# FIAP Games API — Spec

*[Leia em Português](context.pt-BR.md)*

## 1. Overview

A REST API for managing **Users** and **Games**, built as a **modular monolith** on **.NET 10**, following **SOLID** and **Clean Architecture** principles. Persistence is **MongoDB**, accessed through **EF Core**'s Mongo provider. The system must be runnable locally via Docker Compose, ship with CI/CD for GitHub, and be deployable to Azure via Terraform.

## 2. Architecture

- **Modular monolith**: one module per bounded context (`Users`, `Games`), each implementing a shared `IModule` contract (`RegisterModule` + `MapEndpoints`). Modules never reference each other directly — only a shared kernel and shared infrastructure library.
- **Clean Architecture layering inside each module**: `Domain` → `Application` → `Infrastructure`, with `Endpoints` as the outermost layer. Dependencies point inward; `Infrastructure` implements abstractions defined in `Application`.
- **Minimal APIs**, not MVC controllers. Each module maps its own routes via `IEndpointRouteBuilder`, grouped with `MapGroup("/api/...")`.
- **SOLID** is applied concretely as: one responsibility per service/repository (SRP), new behavior added via new implementations rather than editing existing ones where reasonable (OCP), repository/service abstractions are substitutable (LSP), narrow interfaces per concern instead of one god-interface (ISP), and modules/endpoints depend on abstractions (`IUserRepository`, `ITokenService`, etc.) injected via DI, never on concrete infrastructure types (DIP).

## 3. Domain model

### User
- `Id` (Guid), `Name`, `Email` (unique), `PasswordHash`, `Role` (`Player` | `Admin`), `CreatedAtUtc`, `UpdatedAtUtc`.

### Game
- `Id` (Guid), `Title`, `Genre`, `Platform`, `Description`, `Price`, `ReleaseDate`, `CreatedAtUtc`, `UpdatedAtUtc`.

## 4. Functional requirements

### Authentication
- Register (`POST /api/users/register`) and login (`POST /api/users/login`) are public.
- Login returns a JWT (with expiry) on valid credentials; passwords are stored hashed, never in plaintext.
- Every other endpoint requires a valid bearer JWT; missing/invalid tokens return `401`.

### Users CRUD
- Get current authenticated user (`GET /api/users/me`).
- Get by id, paginated list, update, delete — all JWT-protected.
- Not-found, validation, and conflict (duplicate email) cases return the correct HTTP status, not a generic error.

### Games CRUD
- Full CRUD (create, get by id, paginated list, update, delete), all JWT-protected.
- Same error-mapping expectations as Users (404/400/409 handled explicitly, not as 500s).

### Pagination
- Every list endpoint accepts `page` and `pageSize` query parameters and returns total count, current page, page size, and previous/next-page flags alongside the items.

## 5. Non-functional requirements

- **Validation**: request DTOs are validated before reaching domain/service logic; invalid input returns `400` with field-level errors.
- **Global error handling**: unhandled exceptions never leak stack traces to the client. They're caught by a single global handler, logged with full detail server-side, and returned as a generic `ProblemDetails` `500` with a trace id for correlation.
- **Structured logging**: every log line is a machine-parseable JSON object, including one structured summary line per HTTP request (method, path, status, duration). Framework noise (default request/EF logging) is dialed down so real signal isn't buried.
- **Result pattern**: expected failures (not found, validation, conflict, unauthorized) are modeled explicitly in the application layer and mapped to HTTP status codes — they are not exceptions and do not go through the global exception handler.

## 6. Data & migrations

- MongoDB is schemaless — there's no relational-style schema migration. Each module owns its own migration classes (index creation, seed data) that run once, in order, tracked in a dedicated history collection, applied automatically at API startup. No manual migration command is required to run the app.
- Migrations exist only for what Mongo doesn't do automatically: unique constraints (e.g. `email`), secondary indexes for query performance, and one-off seed data — not for defining a collection's shape.

## 7. Infrastructure & deployment

- **Docker**: a multi-stage Dockerfile (SDK build stage → ASP.NET runtime stage), running as a non-root user, listening on a fixed internal port.
- **docker-compose**: brings up the API and MongoDB together, with a health check gating API startup on Mongo being ready. Secrets (JWT signing key, Mongo credentials) are supplied via environment variables, never hardcoded.
- **CI/CD (GitHub Actions)**: every push/PR builds, restores, and runs the full test suite. On push to the main branch, the API image is additionally built and published to a container registry, tagged with both the commit SHA and `latest`.
- **Infrastructure as code (Terraform, Azure)**: a script that can rebuild the required cloud infrastructure from scratch given an Azure account — compute to run the container image, a managed MongoDB-compatible database, and the supporting resources (resource group, logging) needed to operate it. Applying it must not require hand-editing resources in the Azure portal.

## 8. Testing

- Unit tests cover application services (business logic, mocked dependencies) and request validators for both modules.
- Tests must not depend on a real MongoDB instance or network access.

## 9. Out of scope

- No frontend/UI.
- No multi-tenancy.
- No automatic/scaffolded Mongo migrations (relational-style `migrations add` tooling doesn't apply here — see §6).
- No real-time features (websockets/SignalR).

## 10. Acceptance criteria

- [ ] `dotnet build` succeeds with 0 warnings/errors.
- [ ] `dotnet test` passes for both modules.
- [ ] `docker compose up --build` brings up a working stack reachable on a documented port, with Swagger available.
- [ ] Full flow works end-to-end: register → login → authenticated CRUD on Users and Games → pagination → 401 on missing auth → 404 on missing resource.
- [ ] An unhandled exception returns a clean `500` with no stack trace to the client, while being fully logged server-side.
- [ ] CI runs build + tests on every push/PR, and publishes a container image on push to main.
- [ ] `terraform plan`/`apply` (with valid Azure credentials) provisions a working environment from scratch.
