# FIAP Games API — Project Documentation

*[Leia em Português](DOCUMENTATION.pt-BR.md)*

## 1. Introduction

This document describes the solution built for the challenge of implementing a REST API to manage **Users** and **Games**, complete with authentication, persistence, and infrastructure. The focus here isn't just "what was built," but **how** it was built: the development methodology and the architectural decisions behind the project.

### Navigation

All of the project's written content is spread across this document and a few others, each with its own purpose:

| File | Content |
|---|---|
| [`README`](../README.md) / [`README.pt-BR`](../README.pt-BR.md) | Quick-start guide to run and evaluate the project (Docker Compose, image pull, tests). |
| [`context`](context/context.md) / [`context.pt-BR`](context/context.pt-BR.md) | Specification: the system's functional and non-functional requirements. |
| [`behavior`](behavior/behavior.md) / [`behavior.pt-BR`](behavior/behavior.pt-BR.md) | Gherkin behavior scenarios (BDD) — the project's acceptance layer. |
| [`discover`](discovers/discover.md) / [`discover.pt-BR`](discovers/discover.pt-BR.md) | Technical concepts and trivia discovered along the way (Shared Kernel, CLR/JIT, .NET history, etc.). |
| [`DiagramaDDD`](diagrams/DiagramaDDD.jpg) | Architecture diagram: bounded contexts, layers, and the Shared Kernel/Infrastructure. |
| [`DiagramaEventStorming`](diagrams/DiagramaEventStorming.jpg) | Event Storming diagram for the user registration and game creation flows. |

## 2. Development methodology

The project was guided by three mutually reinforcing methodological pillars, applied throughout the whole development cycle: **DDD** to model the domain, **Clean Architecture** (with **SOLID**) to organize the code, and **BDD** to describe and validate the system's expected behavior.

### 2.1 Domain-Driven Design (DDD)

The problem domain was split into clear **bounded contexts** — `Users` and `Games` — each treated as an independent module with its own vocabulary and its own rules. This separation avoids the tight coupling typical of systems where everything knows about everything, and directly mirrors how the business actually thinks about these two concepts: users and games have no reason to know each other's internal details.

Domain entities (`User`, `Game`) carry behavior — they aren't just property bags. Rules like "update profile" or "change password" live on the entity itself, not scattered across the application layer.

Each bounded context's flow was first mapped out with **Event Storming** — actors, commands, validation rules/policies, aggregates, and domain events — before it became code. The result is in [`DiagramaEventStorming`](diagrams/DiagramaEventStorming.jpg), covering the user registration flow and the game creation flow (diagram labels are in Portuguese).

### 2.2 Clean Architecture and SOLID

Within each module, code is organized in concentric layers — `Domain` → `Application` → `Infrastructure`, with `Endpoints` as the outermost layer. The rule is simple: dependencies always point inward. The domain doesn't know about the database, the application layer doesn't know about HTTP details, and infrastructure only implements contracts defined by the inner layers.

**SOLID** shows up concretely in this organization:
- **S**RP — each service and repository has a single reason to change.
- **O**CP — new behavior is added via new implementations, without rewriting what already works.
- **L**SP — repository/service abstractions can be swapped without breaking their consumers.
- **I**SP — small, focused interfaces (`IUserRepository`, `ITokenService`, `IPasswordHasher`) instead of one interface that does everything.
- **D**IP — modules depend on abstractions injected via DI, never on concrete infrastructure implementations.

### 2.3 Behavior-Driven Development (BDD)

Before (and alongside) implementation, the API's expected behavior was written in **Gherkin** format — `Given / When / Then` — in [`behavior`](behavior/behavior.md) / [`behavior.pt-BR`](behavior/behavior.pt-BR.md). These scenarios act as the project's acceptance layer: they describe what the system should do from the perspective of an API consumer (registering a user, authenticating, listing paginated games, getting a 401 without a token, etc.), without diving into implementation detail.

This behavior documentation is paired with a written specification ([`context`](context/context.md) / [`context.pt-BR`](context/context.pt-BR.md)) that formalizes the system's functional and non-functional requirements. Together, these two documents drove the architectural decisions and the acceptance criteria used to validate the implementation.

## 3. Solution architecture

The system is a **modular monolith**: a single deployable application, internally split into independent modules that share only two libraries (a framework-agnostic kernel and a shared infrastructure layer).

```
src/
  Api/FiapGames.Api                     # host: wires modules, authentication, Swagger, health checks
  Shared/FiapGames.Shared.Kernel        # framework-agnostic building blocks (Entity, Result, Pagination)
  Shared/FiapGames.Shared.Infrastructure# Mongo, JWT, migrations — shared infrastructure
  Modules/Users/FiapGames.Modules.Users # Domain / Application / Infrastructure / Endpoints
  Modules/Games/FiapGames.Modules.Games # same layering as Users
tests/
  FiapGames.Modules.Users.Tests
  FiapGames.Modules.Games.Tests
infra/terraform/                        # Azure infrastructure as code
```

Each module implements a shared contract (`IModule`, with `RegisterModule` and `MapEndpoints`) and is registered with the host — modules never reference each other directly. Endpoints are built with **Minimal APIs**, grouped per module (`MapGroup("/api/...")`), with authentication required per group via `RequireAuthorization()`.

The diagram in [`DiagramaDDD`](diagrams/DiagramaDDD.jpg) shows this same structure visually: the two bounded contexts, their internal layers (Endpoints → Application → Domain, with Infrastructure implementing persistence), and the Shared Kernel/Infrastructure both contexts depend on (diagram labels are in Portuguese).

## 4. Domain model

| Entity | Main fields |
|---|---|
| **User** | Id, Name, Email (unique), PasswordHash, Role (`Player`/`Admin`), CreatedAtUtc, UpdatedAtUtc |
| **Game** | Id, Title, Genre, Platform, Description, Price, ReleaseDate, CreatedAtUtc, UpdatedAtUtc |

## 5. Technology stack

- **.NET 10** — an **LTS** (Long-Term Support) release, officially supported by Microsoft through November 2028.
- **MongoDB** via **EF Core** (the official `MongoDB.EntityFrameworkCore` provider).
- **JWT** for authentication, with passwords protected by hashing (BCrypt).
- **FluentValidation** for input validation.
- **Serilog** for structured logging + global exception handling.
- **xUnit** + **NSubstitute** for automated tests.
- **Docker** and **docker-compose** for containerized local execution.
- **GitHub Actions** for CI/CD.
- **Terraform** for infrastructure provisioning on Azure.

## 6. Persistence

MongoDB is schemaless, so there's no relational-style schema migration. What the project created were **index migrations**: a unique index on `users.email` (enforcing account uniqueness) and secondary indexes on `games.title` and `games.genre` (for query performance). These migrations run automatically at API startup — no manual command is required.

## 7. Quality: tests, error handling and observability

- **Automated tests**: cover application services and validators for both modules, with mocked dependencies (no dependency on a real database).
- **Behavior scenarios (BDD)**: `behavior`/`behavior.pt-BR` document, in Gherkin, the acceptance flows validated manually and automatically throughout development.
- **Global exception handling**: any unexpected error is caught centrally, logged in full detail server-side, and returned to the client as a generic, safe response (no stack trace).
- **Structured logging**: every request produces one JSON log line, ready to be consumed by observability tooling. Application services (`UserService`, `GameService`) also log the relevant business events (registration, login, conflict, not found, create/update/delete) via `ILogger`, with named properties (`{UserId}`, `{Email}`, `{GameId}`) instead of loose strings — that lays the groundwork for a future OpenTelemetry adoption without having to rewrite business logic.

### 7.1 Test coverage

Aggregate line coverage (`dotnet test --collect:"XPlat Code Coverage"`): **32.2%**.

| Project | Coverage |
|---|---|
| `FiapGames.Shared.Kernel` | 77.9% |
| `FiapGames.Modules.Games` | 42.7% |
| `FiapGames.Modules.Users` | 28.2% |
| `FiapGames.Shared.Infrastructure` | 2.8% |

Coverage is concentrated in `Application` (services/validators) and domain entities; `Endpoints`, Mongo repositories, and migrations sit at 0%, validated manually instead of by automated test.

## 8. Deliverables

- A functional REST API with full CRUD for Users and Games, paginated listing, and JWT authentication.
- An automated test suite.
- A written project specification (`context`) and BDD behavior scenarios (`behavior`), both also available in Portuguese.
- Application containerization (Dockerfile + docker-compose).
- A CI/CD pipeline on GitHub Actions (build, test, and image publishing).
- Infrastructure as code in Terraform, to provision the full environment on Azure.
- Project documentation (this document) and a quick-start guide (`README`).

## 9. CI/CD and the published image

### 9.1 Pipeline

The workflow (`.github/workflows/ci-cd.yml`) has two jobs. `build-and-test` runs on every push or pull request to `main`: restore, build, and run the test suite. `docker-build-and-push` only runs on a direct push to `main` (never on PRs) and only after the previous job passes — it builds the API image from the `Dockerfile` and publishes it.

### 9.2 GitHub Container Registry (GHCR)

The image is published to `ghcr.io/kainanguerra/fiap-games`, with two tags on every push: the commit SHA (an immutable, traceable version) and `latest` (always the most recent build). GHCR packages are private by default, but for an academic project it makes sense to make this one public (Package settings → Change visibility) — that way, whoever is evaluating the project can pull and run it directly, with no GitHub credentials involved.

### 9.3 What you can do with the image

With the package public, there are two equally valid ways to run the project: cloning the repository and building with Docker Compose, or pulling the published image straight from GHCR without cloning anything — both are documented side by side in `README`. The same image can also be fed directly into the Terraform `container_image` variable to deploy it to Azure Container Apps.

## 10. Running the project

The full step-by-step for running the project locally (via Docker Compose or natively) is in [`README`](../README.md), written for anyone who just needs to stand it up and evaluate it.

## 11. Conclusion

The project combines three mutually reinforcing practices: **DDD** to model the problem around the business's own language, **Clean Architecture/SOLID** to keep the code organized and testable, and **BDD** to ensure the delivered behavior matches the specified behavior. The result is a modular, tested, containerized API with reproducible infrastructure — from code to cloud.
