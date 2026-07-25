# FIAP Games API — Documentação do Projeto

*[Read in English](DOCUMENTATION.md)*

## 1. Introdução

Este documento descreve a solução desenvolvida para o desafio de construir uma API REST de gerenciamento de **Usuários** e **Jogos**, com autenticação, persistência e infraestrutura completas. O foco aqui não é só "o que foi construído", mas **como** foi construído: a metodologia de desenvolvimento e as decisões de arquitetura por trás do projeto.

## 2. Metodologia de desenvolvimento

O projeto foi guiado por três pilares metodológicos, que se complementam ao longo de todo o ciclo de desenvolvimento: **DDD** para modelar o domínio, **Clean Architecture** (com **SOLID**) para organizar o código, e **BDD** para descrever e validar o comportamento esperado do sistema.

### 2.1 Domain-Driven Design (DDD)

O domínio do problema foi dividido em **bounded contexts** claros — `Users` e `Games` — cada um tratado como um módulo independente, com seu próprio vocabulário e suas próprias regras. Essa separação evita o acoplamento típico de sistemas onde tudo conhece tudo, e reflete diretamente a forma como o negócio pensa sobre esses dois conceitos: usuários e jogos não têm motivo para conhecer os detalhes internos um do outro.

As entidades de domínio (`User`, `Game`) carregam comportamento, não são apenas sacos de propriedades: regras como "atualizar perfil" ou "trocar senha" vivem na própria entidade, e não espalhadas pela camada de aplicação.

Os fluxos de cada bounded context foram primeiro mapeados com **Event Storming** — atores, comandos, regras/políticas de validação, agregados e eventos de domínio — antes de virar código. O resultado está em [`diagrams/DiagramaEventStorming.jpg`](diagrams/DiagramaEventStorming.jpg), cobrindo o fluxo de registro de usuário e o fluxo de criação de jogo.

### 2.2 Clean Architecture e SOLID

Dentro de cada módulo, o código é organizado em camadas concêntricas — `Domain` → `Application` → `Infrastructure`, com os `Endpoints` como camada mais externa. A regra é simples: dependências sempre apontam para dentro. O domínio não conhece o banco de dados, a camada de aplicação não conhece detalhes HTTP, e a infraestrutura apenas implementa contratos definidos pelas camadas internas.

Os princípios **SOLID** aparecem de forma concreta nessa organização:
- **S**RP — cada serviço e repositório tem uma única razão para mudar.
- **O**CP — novo comportamento é adicionado com novas implementações, sem reescrever o que já funciona.
- **L**SP — abstrações de repositório e serviço podem ser substituídas sem quebrar quem as consome.
- **I**SP — interfaces pequenas e específicas (`IUserRepository`, `ITokenService`, `IPasswordHasher`) em vez de uma interface única que faz tudo.
- **D**IP — módulos dependem de abstrações injetadas via DI, nunca de implementações concretas de infraestrutura.

### 2.3 Behavior-Driven Development (BDD)

Antes de (e junto com) a implementação, o comportamento esperado da API foi escrito em formato **Gherkin** — `Dado / Quando / Então` — nos arquivos [`behavior.md`](behavior.md) / [`behavior.pt-BR.md`](behavior.pt-BR.md). Esses cenários funcionam como a camada de aceite do projeto: descrevem o que o sistema deve fazer do ponto de vista de quem consome a API (registrar um usuário, autenticar, listar jogos paginados, receber 401 sem token, etc.), sem entrar em detalhe de implementação.

Essa documentação de comportamento é acompanhada por uma especificação escrita ([`context.md`](context.md) / [`context.pt-BR.md`](context.pt-BR.md)), que formaliza os requisitos funcionais e não funcionais do sistema. Juntos, esses dois documentos guiaram as decisões de arquitetura e os critérios de aceite usados para validar a implementação.

## 3. Arquitetura da solução

O sistema é um **monólito modular**: uma única aplicação implantável, mas internamente dividida em módulos independentes que compartilham apenas duas bibliotecas (um *kernel* agnóstico de framework e uma camada de infraestrutura compartilhada).

```
src/
  Api/FiapGames.Api                     # host: registra módulos, autenticação, Swagger, health checks
  Shared/FiapGames.Shared.Kernel        # blocos agnósticos de framework (Entity, Result, Paginação)
  Shared/FiapGames.Shared.Infrastructure# Mongo, JWT, migrations — infraestrutura compartilhada
  Modules/Users/FiapGames.Modules.Users # Domain / Application / Infrastructure / Endpoints
  Modules/Games/FiapGames.Modules.Games # mesma organização de Users
tests/
  FiapGames.Modules.Users.Tests
  FiapGames.Modules.Games.Tests
infra/terraform/                        # infraestrutura como código para Azure
```

Cada módulo implementa um contrato comum (`IModule`, com `RegisterModule` e `MapEndpoints`) e é registrado no host — os módulos nunca se referenciam diretamente entre si. Os endpoints são construídos com **Minimal APIs**, agrupados por módulo (`MapGroup("/api/...")`), com autenticação exigida por grupo via `RequireAuthorization()`.

O diagrama em [`diagrams/DiagramaDDD.jpg`](diagrams/DiagramaDDD.jpg) mostra essa mesma estrutura visualmente: os dois bounded contexts, suas camadas internas (Endpoints → Application → Domain, com a Infrastructure implementando a persistência) e o Shared Kernel/Infrastructure compartilhado por ambos.

## 4. Modelo de domínio

| Entidade | Campos principais |
|---|---|
| **User** | Id, Name, Email (único), PasswordHash, Role (`Player`/`Admin`), CreatedAtUtc, UpdatedAtUtc |
| **Game** | Id, Title, Genre, Platform, Description, Price, ReleaseDate, CreatedAtUtc, UpdatedAtUtc |

## 5. Stack tecnológica

- **.NET 10** — versão **LTS** (Long-Term Support), com suporte oficial da Microsoft previsto até novembro de 2028.
- **MongoDB** via **EF Core** (provider oficial `MongoDB.EntityFrameworkCore`).
- **JWT** para autenticação, com senhas protegidas por hash (BCrypt).
- **FluentValidation** para validação de entrada.
- **Serilog** para logs estruturados + tratamento global de exceções.
- **xUnit** + **NSubstitute** para testes automatizados.
- **Docker** e **docker-compose** para execução local containerizada.
- **GitHub Actions** para CI/CD.
- **Terraform** para provisionamento de infraestrutura no Azure.

## 6. Persistência

O MongoDB é schemaless, então não existe migration de schema no sentido relacional. O que o projeto criou foram **migrations de índices**: um índice único em `users.email` (garantindo unicidade de conta) e índices secundários em `games.title` e `games.genre` (para performance de consulta). Essas migrations rodam automaticamente na inicialização da API — não é necessário nenhum comando manual.

## 7. Qualidade: testes, erros e observabilidade

- **Testes automatizados**: cobertura dos serviços de aplicação e dos validadores de ambos os módulos, com dependências mockadas (sem depender de banco real).
- **Cenários de comportamento (BDD)**: os arquivos `behavior.md`/`behavior.pt-BR.md` documentam, em Gherkin, os fluxos de aceite validados manual e automaticamente durante o desenvolvimento.
- **Tratamento global de exceções**: qualquer erro não esperado é capturado centralmente, logado com detalhe no servidor, e retornado ao cliente como uma resposta genérica e segura (sem stack trace).
- **Logs estruturados**: cada requisição gera uma linha de log em JSON, pronta para ser consumida por ferramentas de observabilidade.

## 8. Entregáveis

- API REST funcional com CRUD completo de Usuários e Jogos, listagem paginada e autenticação JWT.
- Suíte de testes automatizados.
- Especificação escrita do projeto (`context.md`) e cenários de comportamento em BDD (`behavior.md`), ambos também em português.
- Containerização da aplicação (Dockerfile + docker-compose).
- Pipeline de CI/CD no GitHub Actions (build, testes e publicação de imagem).
- Infraestrutura como código em Terraform, para provisionar o ambiente completo no Azure.
- Documentação do projeto (este documento) e um guia rápido de execução (`README.md`).

## 9. CI/CD e a imagem publicada

### 9.1 Pipeline

O workflow (`.github/workflows/ci-cd.yml`) tem dois jobs. `build-and-test` roda em todo push ou pull request para a `main`: restaura, compila e executa a suíte de testes. `docker-build-and-push` só roda em push direto na `main` (nunca em PRs) e só depois que o job anterior passa — ele builda a imagem da API a partir do `Dockerfile` e publica.

### 9.2 GitHub Container Registry (GHCR)

A imagem é publicada em `ghcr.io/kainanguerra/fiap-games`, com duas tags a cada push: o SHA do commit (versão imutável e rastreável) e `latest` (sempre a build mais recente). Pacotes no GHCR nascem privados por padrão, mas para um projeto acadêmico faz sentido tornar esse pacote público (em Settings do pacote → Change visibility) — assim quem for avaliar o projeto consegue puxar e rodar a imagem diretamente, sem precisar de nenhuma credencial do GitHub.

### 9.3 O que dá pra fazer com a imagem

Com o pacote público, existem duas formas igualmente válidas de rodar o projeto: clonando o repositório e buildando com Docker Compose, ou puxando a imagem já publicada direto do GHCR, sem clonar nada — as duas opções estão documentadas lado a lado no `README.pt-BR.md`. A mesma imagem também pode ser apontada diretamente na variável `container_image` do Terraform para implantar no Azure Container Apps.

## 10. Como executar

O passo a passo completo para rodar o projeto localmente (via Docker Compose ou nativamente) está no [`README.md`](README.md), pensado para quem só precisa subir e avaliar a aplicação.

## 11. Conclusão

O projeto combina três práticas que se reforçam mutuamente: **DDD** para modelar o problema em torno da linguagem do negócio, **Clean Architecture/SOLID** para manter o código organizado e testável, e **BDD** para garantir que o comportamento entregue corresponda ao comportamento especificado. O resultado é uma API modular, testada, containerizada e com infraestrutura reproduzível — do código à nuvem.
