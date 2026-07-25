# FIAP Cloud Games API

*[Leia em Português](README.pt-BR.md)*

A .NET 10 REST API for managing Users and Games, with JWT authentication and MongoDB persistence via EF Core, organized as a modular monolith.

## Running the project

### Prerequisites

- Docker and Docker Compose (all you need to run the application)
- .NET 10 SDK (optional — only needed to run the API outside Docker or run tests locally)

### Running with Docker Compose

Copy the environment variables file for your operating system, then bring up the stack:

**Linux / macOS**
```bash
cp .env.example .env
docker compose up --build
```

**Windows (PowerShell)**
```powershell
Copy-Item .env.example .env
docker compose up --build
```

**Windows (Command Prompt / cmd.exe)**
```cmd
copy .env.example .env
docker compose up --build
```

This brings up the API and MongoDB together. Wait a few seconds for Mongo to become healthy and the API to start.

- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Health check: http://localhost:8080/health

### Testing the API through Swagger

1. Open http://localhost:8080/swagger.
2. Create an account at `POST /api/users/register`.
3. Log in at `POST /api/users/login` to get a JWT token.
4. Click **Authorize** at the top of Swagger and paste the token (format `Bearer <token>`).
5. Explore the remaining Users and Games endpoints — authenticated ones show a lock icon.

### Running without Docker (local API + Mongo in a container)

```bash
docker run -d --name mongo -p 27017:27017 mongo:7
dotnet run --project src/Api/FiapGames.Api
```

A default JWT secret already exists in `appsettings.Development.json` for this mode, no extra configuration needed.

### Running the automated tests

```bash
dotnet test
```

21 unit tests covering the services and validators of the Users and Games modules.

## Stack

- .NET 10 / ASP.NET Core Minimal APIs
- MongoDB with EF Core (via `MongoDB.EntityFrameworkCore`)
- JWT authentication
- FluentValidation
- Serilog (structured logging) + global exception handling
- Docker / docker-compose
- GitHub Actions (CI/CD)
- Terraform (Azure infrastructure)

## API surface

| Method | Route                | Auth | Description |
|--------|-----------------------|------|--------------|
| POST   | `/api/users/register`  | none | Create an account |
| POST   | `/api/users/login`     | none | Exchange credentials for a JWT |
| GET    | `/api/users/me`        | JWT  | Current user's claims |
| GET    | `/api/users/{id}`      | JWT  | Get a user |
| GET    | `/api/users?page=&pageSize=` | JWT | Paginated user list |
| PUT    | `/api/users/{id}`      | JWT  | Update a user |
| DELETE | `/api/users/{id}`      | JWT  | Delete a user |
| POST   | `/api/games`           | JWT  | Create a game |
| GET    | `/api/games/{id}`      | JWT  | Get a game |
| GET    | `/api/games?page=&pageSize=` | JWT | Paginated game list |
| PUT    | `/api/games/{id}`      | JWT  | Update a game |
| DELETE | `/api/games/{id}`      | JWT  | Delete a game |

## Full documentation

For architecture details (module organization, design decisions, Mongo migrations, CI/CD, and Terraform infrastructure), see [DOCUMENTATION.md](DOCUMENTATION.md) (or [DOCUMENTATION.pt-BR.md](DOCUMENTATION.pt-BR.md) in Portuguese).
