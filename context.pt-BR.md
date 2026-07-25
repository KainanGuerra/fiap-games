# FIAP Games API — Especificação

*[Read in English](context.md)*

## 1. Visão geral

Uma API REST para gerenciar **Usuários** e **Jogos**, construída como um **monólito modular** em **.NET 10**, seguindo os princípios **SOLID** e **Clean Architecture**. A persistência é feita em **MongoDB**, acessado através do provider Mongo do **EF Core**. O sistema precisa rodar localmente via Docker Compose, vir com CI/CD para GitHub, e ser implantável no Azure via Terraform.

## 2. Arquitetura

- **Monólito modular**: um módulo por contexto delimitado (`Users`, `Games`), cada um implementando um contrato compartilhado `IModule` (`RegisterModule` + `MapEndpoints`). Módulos nunca se referenciam diretamente entre si — apenas um kernel e uma biblioteca de infraestrutura compartilhados.
- **Camadas de Clean Architecture dentro de cada módulo**: `Domain` → `Application` → `Infrastructure`, com `Endpoints` como a camada mais externa. As dependências apontam para dentro; `Infrastructure` implementa abstrações definidas em `Application`.
- **Minimal APIs**, não controllers MVC. Cada módulo mapeia suas próprias rotas via `IEndpointRouteBuilder`, agrupadas com `MapGroup("/api/...")`.
- **SOLID** é aplicado de forma concreta assim: uma responsabilidade por serviço/repositório (SRP), novo comportamento é adicionado via novas implementações em vez de editar as existentes quando razoável (OCP), abstrações de repositório/serviço são substituíveis (LSP), interfaces estreitas por preocupação em vez de uma interface "faz tudo" (ISP), e módulos/endpoints dependem de abstrações (`IUserRepository`, `ITokenService`, etc.) injetadas via DI, nunca de tipos concretos de infraestrutura (DIP).

## 3. Modelo de domínio

### User
- `Id` (Guid), `Name`, `Email` (único), `PasswordHash`, `Role` (`Player` | `Admin`), `CreatedAtUtc`, `UpdatedAtUtc`.

### Game
- `Id` (Guid), `Title`, `Genre`, `Platform`, `Description`, `Price`, `ReleaseDate`, `CreatedAtUtc`, `UpdatedAtUtc`.

## 4. Requisitos funcionais

### Autenticação
- Registro (`POST /api/users/register`) e login (`POST /api/users/login`) são públicos.
- Login retorna um JWT (com expiração) quando as credenciais são válidas; senhas são armazenadas com hash, nunca em texto puro.
- Todo outro endpoint exige um JWT válido no header; token ausente/inválido retorna `401`.

### CRUD de Usuários
- Buscar o usuário autenticado atual (`GET /api/users/me`).
- Buscar por id, listagem paginada, atualizar, remover — tudo protegido por JWT.
- Casos de não encontrado, validação e conflito (email duplicado) retornam o status HTTP correto, não um erro genérico.

### CRUD de Jogos
- CRUD completo (criar, buscar por id, listagem paginada, atualizar, remover), tudo protegido por JWT.
- Mesmas expectativas de mapeamento de erro que Usuários (404/400/409 tratados explicitamente, não como 500).

### Paginação
- Todo endpoint de listagem aceita os parâmetros de query `page` e `pageSize` e retorna a contagem total, a página atual, o tamanho de página e as flags de página anterior/próxima junto com os itens.

## 5. Requisitos não funcionais

- **Validação**: os DTOs de requisição são validados antes de chegar à lógica de domínio/serviço; entrada inválida retorna `400` com erros por campo.
- **Tratamento global de erros**: exceções não tratadas nunca vazam stack trace para o cliente. Elas são capturadas por um único handler global, logadas com todo o detalhe no servidor, e retornadas como um `ProblemDetails` genérico `500` com um trace id para correlação.
- **Logs estruturados**: cada linha de log é um objeto JSON legível por máquina, incluindo uma linha de resumo estruturada por requisição HTTP (método, rota, status, duração). O ruído do próprio framework (logging padrão de requisição/EF) é reduzido para que o sinal real não fique enterrado.
- **Padrão Result**: falhas esperadas (não encontrado, validação, conflito, não autorizado) são modeladas explicitamente na camada de aplicação e mapeadas para códigos HTTP — elas não são exceções e não passam pelo handler global de exceções.

## 6. Dados e migrations

- O MongoDB não tem schema fixo — não existe migration de schema no estilo relacional. Cada módulo tem suas próprias classes de migration (criação de índices, seed de dados) que rodam uma única vez, em ordem, com o histórico registrado em uma coleção dedicada, aplicadas automaticamente na inicialização da API. Nenhum comando manual de migration é necessário para rodar a aplicação.
- Migrations existem apenas para o que o Mongo não faz automaticamente: constraints de unicidade (ex.: `email`), índices secundários para performance de consulta, e dados de seed pontuais — não para definir o formato de uma coleção.

## 7. Infraestrutura e deploy

- **Docker**: um Dockerfile multi-estágio (estágio de build com o SDK → estágio de runtime do ASP.NET), rodando como usuário não-root, escutando em uma porta interna fixa.
- **docker-compose**: sobe a API e o MongoDB juntos, com um health check que condiciona a inicialização da API à disponibilidade do Mongo. Segredos (chave de assinatura do JWT, credenciais do Mongo) são fornecidos via variáveis de ambiente, nunca hardcoded.
- **CI/CD (GitHub Actions)**: todo push/PR compila, restaura e roda a suíte de testes completa. Em push para a branch principal, a imagem da API também é compilada e publicada em um container registry, com tags do SHA do commit e `latest`.
- **Infraestrutura como código (Terraform, Azure)**: um script que consegue reconstruir do zero a infraestrutura de nuvem necessária, dada uma conta Azure — computação para rodar a imagem do container, um banco compatível com MongoDB gerenciado, e os recursos de suporte (resource group, logging) necessários para operá-la. Aplicá-lo não deve exigir edição manual de recursos no portal do Azure.

## 8. Testes

- Testes unitários cobrem os serviços de aplicação (lógica de negócio, dependências mockadas) e os validadores de requisição de ambos os módulos.
- Os testes não podem depender de uma instância real do MongoDB nem de acesso à rede.

## 9. Fora de escopo

- Sem frontend/UI.
- Sem multi-tenancy.
- Sem migrations automáticas/geradas para o Mongo (ferramentas no estilo relacional `migrations add` não se aplicam aqui — ver §6).
- Sem recursos em tempo real (websockets/SignalR).

## 10. Critérios de aceitação

- [ ] `dotnet build` termina com 0 warnings/erros.
- [ ] `dotnet test` passa para ambos os módulos.
- [ ] `docker compose up --build` sobe uma stack funcional acessível em uma porta documentada, com Swagger disponível.
- [ ] O fluxo completo funciona de ponta a ponta: registro → login → CRUD autenticado em Usuários e Jogos → paginação → 401 sem autenticação → 404 em recurso inexistente.
- [ ] Uma exceção não tratada retorna um `500` limpo, sem stack trace para o cliente, enquanto é totalmente logada no servidor.
- [ ] O CI roda build + testes em todo push/PR, e publica uma imagem de container em push para a main.
- [ ] `terraform plan`/`apply` (com credenciais Azure válidas) provisiona um ambiente funcional do zero.
