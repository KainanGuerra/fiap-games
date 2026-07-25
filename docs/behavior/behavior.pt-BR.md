# FIAP Games API — Comportamento

*[Read in English](behavior.md)*

Cenários em estilo Gherkin descrevendo o comportamento observável da API. Eles descrevem *o quê* o sistema faz visto de fora (HTTP entra, HTTP sai) — é a camada de aceite que o `context.pt-BR` (a especificação) direciona, não detalhe de implementação.

Os dois fluxos principais (registro de usuário e criação de jogo) foram primeiro mapeados com Event Storming antes de virar cenário — veja [`DiagramaEventStorming`](../diagrams/DiagramaEventStorming.jpg) para ver atores, comandos, regras e eventos de domínio lado a lado.

---

## Funcionalidade: Registro de usuário

```gherkin
Funcionalidade: Registro de usuário
  Como um novo usuário
  Eu quero criar uma conta
  Para que eu possa fazer login e usar a API

  Cenário: Registro bem-sucedido
    Dado que não existe nenhum usuário registrado com o email "player@example.com"
    Quando eu faço POST /api/users/register com nome, email e senha válidos (senha com 8+ caracteres)
    Então o status da resposta é 201 Created
    E o corpo da resposta contém o id, nome, email, role e createdAtUtc do novo usuário
    E a resposta não contém a senha nem o hash da senha

  Cenário: Email duplicado é rejeitado
    Dado que já existe um usuário com o email "player@example.com"
    Quando eu faço POST /api/users/register com o email "player@example.com"
    Então o status da resposta é 409 Conflict

  Esquema do Cenário: Entrada de registro inválida é rejeitada
    Quando eu faço POST /api/users/register com <campo> igual a <valor>
    Então o status da resposta é 400 Bad Request
    E a resposta descreve o erro de validação para <campo>

    Exemplos:
      | campo    | valor                              |
      | name     | "" (vazio)                          |
      | email    | "nao-e-um-email"                    |
      | email    | "" (vazio)                          |
      | password | "" (vazio)                          |
      | password | "curta" (menos de 8 caracteres)      |
```

## Funcionalidade: Autenticação

```gherkin
Funcionalidade: Login e autenticação JWT
  Como um usuário registrado
  Eu quero trocar minhas credenciais por um token
  Para que eu possa chamar endpoints protegidos

  Cenário: Login bem-sucedido
    Dado que um usuário está registrado com o email "player@example.com" e a senha "SuperSecret123!"
    Quando eu faço POST /api/users/login com essas credenciais
    Então o status da resposta é 200 OK
    E o corpo da resposta contém um accessToken e um expiresAtUtc

  Cenário: Login com senha incorreta
    Dado que um usuário está registrado com o email "player@example.com"
    Quando eu faço POST /api/users/login com o email correto e uma senha incorreta
    Então o status da resposta é 401 Unauthorized

  Cenário: Login com email desconhecido
    Quando eu faço POST /api/users/login com um email que não está registrado
    Então o status da resposta é 401 Unauthorized

  Cenário: Chamar um endpoint protegido sem token
    Quando eu faço GET /api/users/me sem o header Authorization
    Então o status da resposta é 401 Unauthorized

  Cenário: Chamar um endpoint protegido com token inválido
    Quando eu faço GET /api/users/me com um bearer token expirado ou malformado
    Então o status da resposta é 401 Unauthorized

  Cenário: Chamar um endpoint protegido com token válido
    Dado que eu fiz login e tenho um access token válido
    Quando eu faço GET /api/users/me com "Authorization: Bearer <token>"
    Então o status da resposta é 200 OK
    E o corpo da resposta contém meu id e email
```

## Funcionalidade: Gerenciamento de usuários

```gherkin
Funcionalidade: CRUD de usuários
  Como um usuário autenticado
  Eu quero ler, atualizar e remover registros de usuário
  Para que os dados da conta permaneçam corretos

  Cenário: Buscar um usuário por id
    Dado que existe um usuário com id "<id>"
    E eu estou autenticado
    Quando eu faço GET /api/users/<id>
    Então o status da resposta é 200 OK
    E o corpo da resposta corresponde a esse usuário

  Cenário: Buscar um usuário que não existe
    Dado que eu estou autenticado
    Quando eu faço GET /api/users/<um id aleatório>
    Então o status da resposta é 404 Not Found

  Cenário: Listar usuários, paginado
    Dado que existem 15 usuários registrados
    E eu estou autenticado
    Quando eu faço GET /api/users?page=2&pageSize=10
    Então o status da resposta é 200 OK
    E a resposta contém 5 itens
    E a resposta reporta totalCount=15, page=2, pageSize=10
    E hasPreviousPage é true e hasNextPage é false

  Cenário: Atualizar um usuário
    Dado que existe um usuário com id "<id>"
    E eu estou autenticado
    Quando eu faço PUT /api/users/<id> com um novo nome e email válidos
    Então o status da resposta é 200 OK
    E um GET /api/users/<id> subsequente reflete a atualização
    E updatedAtUtc é mais recente que createdAtUtc

  Cenário: Remover um usuário
    Dado que existe um usuário com id "<id>"
    E eu estou autenticado
    Quando eu faço DELETE /api/users/<id>
    Então o status da resposta é 204 No Content
    E um GET /api/users/<id> subsequente retorna 404 Not Found
```

## Funcionalidade: Gerenciamento de jogos

```gherkin
Funcionalidade: CRUD de jogos
  Como um usuário autenticado
  Eu quero gerenciar o catálogo de jogos
  Para que jogos possam ser criados, listados, atualizados e removidos

  Cenário: Criar um jogo
    Dado que eu estou autenticado
    Quando eu faço POST /api/games com title, genre, platform válidos, price >= 0 e releaseDate
    Então o status da resposta é 201 Created
    E o corpo da resposta contém o id do novo jogo

  Esquema do Cenário: Entrada de jogo inválida é rejeitada
    Dado que eu estou autenticado
    Quando eu faço POST /api/games com <campo> igual a <valor>
    Então o status da resposta é 400 Bad Request

    Exemplos:
      | campo       | valor                                |
      | title       | "" (vazio)                            |
      | genre       | "" (vazio)                            |
      | platform    | "" (vazio)                            |
      | price       | -1 (negativo)                          |
      | description | uma string com mais de 2000 caracteres|

  Cenário: Buscar um jogo por id
    Dado que existe um jogo com id "<id>"
    E eu estou autenticado
    Quando eu faço GET /api/games/<id>
    Então o status da resposta é 200 OK

  Cenário: Buscar um jogo que não existe
    Dado que eu estou autenticado
    Quando eu faço GET /api/games/<um id aleatório>
    Então o status da resposta é 404 Not Found

  Cenário: Listar jogos, paginado
    Dado que existem 25 jogos no catálogo
    E eu estou autenticado
    Quando eu faço GET /api/games?page=1&pageSize=10
    Então o status da resposta é 200 OK
    E a resposta contém 10 itens
    E hasNextPage é true

  Cenário: Atualizar um jogo
    Dado que existe um jogo com id "<id>"
    E eu estou autenticado
    Quando eu faço PUT /api/games/<id> com campos válidos
    Então o status da resposta é 200 OK
    E um GET /api/games/<id> subsequente reflete a atualização

  Cenário: Remover um jogo
    Dado que existe um jogo com id "<id>"
    E eu estou autenticado
    Quando eu faço DELETE /api/games/<id>
    Então o status da resposta é 204 No Content
    E um GET /api/games/<id> subsequente retorna 404 Not Found

  Cenário: Todo endpoint de jogos exige autenticação
    Quando eu chamo qualquer endpoint /api/games sem bearer token
    Então o status da resposta é 401 Unauthorized
```

## Funcionalidade: Tratamento global de erros

```gherkin
Funcionalidade: Tratamento global de erros
  Como consumidor da API
  Eu quero que falhas voltem como respostas previsíveis e seguras
  Para que eu nunca veja um stack trace vazado ou um travamento opaco

  Cenário: Uma exceção inesperada não vaza detalhes internos
    Dado que ocorre uma exceção não tratada durante o processamento de uma requisição
    Quando a resposta é retornada ao cliente
    Então o status da resposta é 500
    E o corpo da resposta é um payload ProblemDetails genérico com um traceId
    E o corpo da resposta não contém stack trace nem mensagem da exceção
    E a exceção completa, incluindo stack trace, é registrada nos logs estruturados

  Cenário: Falhas esperadas não são tratadas como erro de servidor
    Dado que uma requisição falha na validação, ou aponta para um recurso inexistente, ou conflita com dados existentes
    Quando a resposta é retornada ao cliente
    Então o código de status reflete a falha (400, 404 ou 409)
    E nenhuma exceção é logada, pois isso é comportamento esperado da aplicação, não um bug

  Cenário: Toda requisição HTTP produz uma linha de log estruturada
    Quando qualquer requisição é tratada, com sucesso ou não
    Então exatamente uma linha de log JSON é emitida resumindo método, rota, status e tempo decorrido
```

## Funcionalidade: Saúde e prontidão da plataforma

```gherkin
Funcionalidade: Health check
  Como um operador
  Eu quero um endpoint de liveness
  Para que orquestradores (Docker Compose, Azure Container Apps) saibam quando a API está pronta

  Cenário: Health check com as dependências no ar
    Dado que o MongoDB está acessível
    Quando eu faço GET /health
    Então o status da resposta é 200 OK
    E o corpo reporta "Healthy"
```

## Funcionalidade: Migrations na inicialização

```gherkin
Funcionalidade: Migrations automáticas do Mongo
  Como um operador
  Eu quero que os índices necessários existam sem um passo manual
  Para que implantar um ambiente novo não exija rodar comandos manualmente

  Cenário: Primeira inicialização contra um banco vazio
    Dado um banco MongoDB novo e vazio
    Quando a API inicia pela primeira vez
    Então as coleções users e games existem antes de qualquer requisição ser atendida
    E existe um índice único em users.email
    E existem índices em games.title e games.genre

  Cenário: Migrations rodam apenas uma vez
    Dado que a API já iniciou uma vez contra um banco
    Quando a API reinicia contra o mesmo banco
    Então migrations já aplicadas não são reaplicadas
```
