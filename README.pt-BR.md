# FIAP Cloud Games API

*[Read in English](README.md)*

API REST em .NET 10 para gerenciamento de Usuários e Jogos, com autenticação JWT e persistência em MongoDB via EF Core, organizada como monólito modular.

## Como rodar o projeto

### Pré-requisitos

- Docker e Docker Compose (é tudo que você precisa para subir a aplicação)
- .NET 10 SDK (opcional — só necessário para rodar a API fora do Docker ou rodar os testes localmente)

### Subindo com Docker Compose

Copie o arquivo de variáveis de ambiente de acordo com o seu sistema operacional, depois suba a stack:

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

**Windows (Prompt de Comando / cmd.exe)**
```cmd
copy .env.example .env
docker compose up --build
```

Isso sobe a API e o MongoDB juntos. Aguarde alguns segundos até o Mongo ficar saudável e a API subir.

- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Health check: http://localhost:8080/health

### Testando a API pelo Swagger

1. Abra http://localhost:8080/swagger.
2. Crie uma conta em `POST /api/users/register`.
3. Faça login em `POST /api/users/login` para obter um token JWT.
4. Clique em **Authorize** no topo do Swagger e cole o token (formato `Bearer <token>`).
5. Explore os demais endpoints de Usuários e Jogos — todos autenticados aparecem com o cadeado.

### Rodando sem Docker (API local + Mongo em container)

```bash
docker run -d --name mongo -p 27017:27017 mongo:7
dotnet run --project src/Api/FiapGames.Api
```

Já existe um segredo de JWT padrão em `appsettings.Development.json` para esse modo, sem necessidade de configuração extra.

### Rodando os testes automatizados

```bash
dotnet test
```

21 testes unitários cobrindo os serviços e validadores dos módulos de Usuários e Jogos.

## Stack utilizada

- .NET 10 / ASP.NET Core Minimal APIs
- MongoDB com EF Core (via `MongoDB.EntityFrameworkCore`)
- Autenticação JWT
- FluentValidation
- Serilog (logs estruturados) + tratamento global de exceções
- Docker / docker-compose
- GitHub Actions (CI/CD)
- Terraform (infraestrutura no Azure)

## Superfície da API

| Método | Rota                | Auth | Descrição |
|--------|-----------------------|------|--------------|
| POST   | `/api/users/register`  | não | Cria uma conta |
| POST   | `/api/users/login`     | não | Troca credenciais por um JWT |
| GET    | `/api/users/me`        | JWT  | Claims do usuário autenticado |
| GET    | `/api/users/{id}`      | JWT  | Busca um usuário |
| GET    | `/api/users?page=&pageSize=` | JWT | Lista paginada de usuários |
| PUT    | `/api/users/{id}`      | JWT  | Atualiza um usuário |
| DELETE | `/api/users/{id}`      | JWT  | Remove um usuário |
| POST   | `/api/games`           | JWT  | Cria um jogo |
| GET    | `/api/games/{id}`      | JWT  | Busca um jogo |
| GET    | `/api/games?page=&pageSize=` | JWT | Lista paginada de jogos |
| PUT    | `/api/games/{id}`      | JWT  | Atualiza um jogo |
| DELETE | `/api/games/{id}`      | JWT  | Remove um jogo |

## Documentação completa

Para detalhes de arquitetura (organização dos módulos, decisões de design, migrations no Mongo, CI/CD e infraestrutura Terraform), veja [DOCUMENTATION.pt-BR.md](DOCUMENTATION.pt-BR.md) (ou [DOCUMENTATION.md](DOCUMENTATION.md) em inglês).
