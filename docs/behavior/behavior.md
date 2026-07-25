# FIAP Games API — Behavior

*[Leia em Português](behavior.pt-BR.md)*

Gherkin-style scenarios describing the observable behavior of the API. These describe *what* the system does from the outside (HTTP in, HTTP out) — they're the acceptance layer that `context` (the spec) drives, not implementation detail.

The two main flows (user registration and game creation) were first mapped out with Event Storming before becoming scenarios — see [`DiagramaEventStorming`](../diagrams/DiagramaEventStorming.jpg) for actors, commands, rules, and domain events side by side (diagram labels are in Portuguese).

---

## Feature: User registration

```gherkin
Feature: User registration
  As a new user
  I want to create an account
  So that I can log in and use the API

  Scenario: Successful registration
    Given no user is registered with the email "player@example.com"
    When I POST /api/users/register with a valid name, email, and password of at least 8 characters
    Then the response status is 201 Created
    And the response body contains the new user's id, name, email, role, and createdAtUtc
    And the response does not contain the password or password hash

  Scenario: Duplicate email is rejected
    Given a user already exists with the email "player@example.com"
    When I POST /api/users/register with the email "player@example.com"
    Then the response status is 409 Conflict

  Scenario Outline: Invalid registration input is rejected
    When I POST /api/users/register with <field> set to <value>
    Then the response status is 400 Bad Request
    And the response describes the validation error for <field>

    Examples:
      | field    | value                          |
      | name     | "" (empty)                      |
      | email    | "not-an-email"                  |
      | email    | "" (empty)                      |
      | password | "" (empty)                      |
      | password | "short" (fewer than 8 characters)|
```

## Feature: Authentication

```gherkin
Feature: Login and JWT authentication
  As a registered user
  I want to exchange my credentials for a token
  So that I can call protected endpoints

  Scenario: Successful login
    Given a user is registered with email "player@example.com" and password "SuperSecret123!"
    When I POST /api/users/login with those credentials
    Then the response status is 200 OK
    And the response body contains an accessToken and an expiresAtUtc

  Scenario: Login with wrong password
    Given a user is registered with email "player@example.com"
    When I POST /api/users/login with the correct email and an incorrect password
    Then the response status is 401 Unauthorized

  Scenario: Login with unknown email
    When I POST /api/users/login with an email that is not registered
    Then the response status is 401 Unauthorized

  Scenario: Calling a protected endpoint without a token
    When I GET /api/users/me without an Authorization header
    Then the response status is 401 Unauthorized

  Scenario: Calling a protected endpoint with an invalid token
    When I GET /api/users/me with an expired or malformed bearer token
    Then the response status is 401 Unauthorized

  Scenario: Calling a protected endpoint with a valid token
    Given I have logged in and hold a valid access token
    When I GET /api/users/me with "Authorization: Bearer <token>"
    Then the response status is 200 OK
    And the response body contains my id and email
```

## Feature: User management

```gherkin
Feature: User CRUD
  As an authenticated user
  I want to read, update, and delete user records
  So that account data stays accurate

  Scenario: Get a user by id
    Given a user exists with id "<id>"
    And I am authenticated
    When I GET /api/users/<id>
    Then the response status is 200 OK
    And the response body matches that user

  Scenario: Get a user that does not exist
    Given I am authenticated
    When I GET /api/users/<a random id>
    Then the response status is 404 Not Found

  Scenario: List users, paginated
    Given there are 15 registered users
    And I am authenticated
    When I GET /api/users?page=2&pageSize=10
    Then the response status is 200 OK
    And the response contains 5 items
    And the response reports totalCount=15, page=2, pageSize=10
    And hasPreviousPage is true and hasNextPage is false

  Scenario: Update a user
    Given a user exists with id "<id>"
    And I am authenticated
    When I PUT /api/users/<id> with a new valid name and email
    Then the response status is 200 OK
    And a subsequent GET /api/users/<id> reflects the update
    And updatedAtUtc is more recent than createdAtUtc

  Scenario: Delete a user
    Given a user exists with id "<id>"
    And I am authenticated
    When I DELETE /api/users/<id>
    Then the response status is 204 No Content
    And a subsequent GET /api/users/<id> returns 404 Not Found
```

## Feature: Game management

```gherkin
Feature: Game CRUD
  As an authenticated user
  I want to manage the game catalog
  So that games can be created, browsed, updated, and removed

  Scenario: Create a game
    Given I am authenticated
    When I POST /api/games with a valid title, genre, platform, price >= 0, and releaseDate
    Then the response status is 201 Created
    And the response body contains the new game's id

  Scenario Outline: Invalid game input is rejected
    Given I am authenticated
    When I POST /api/games with <field> set to <value>
    Then the response status is 400 Bad Request

    Examples:
      | field       | value                          |
      | title       | "" (empty)                      |
      | genre       | "" (empty)                      |
      | platform    | "" (empty)                      |
      | price       | -1 (negative)                    |
      | description | a string longer than 2000 chars |

  Scenario: Get a game by id
    Given a game exists with id "<id>"
    And I am authenticated
    When I GET /api/games/<id>
    Then the response status is 200 OK

  Scenario: Get a game that does not exist
    Given I am authenticated
    When I GET /api/games/<a random id>
    Then the response status is 404 Not Found

  Scenario: List games, paginated
    Given there are 25 games in the catalog
    And I am authenticated
    When I GET /api/games?page=1&pageSize=10
    Then the response status is 200 OK
    And the response contains 10 items
    And hasNextPage is true

  Scenario: Update a game
    Given a game exists with id "<id>"
    And I am authenticated
    When I PUT /api/games/<id> with valid fields
    Then the response status is 200 OK
    And a subsequent GET /api/games/<id> reflects the update

  Scenario: Delete a game
    Given a game exists with id "<id>"
    And I am authenticated
    When I DELETE /api/games/<id>
    Then the response status is 204 No Content
    And a subsequent GET /api/games/<id> returns 404 Not Found

  Scenario: Every game endpoint requires authentication
    When I call any /api/games endpoint without a bearer token
    Then the response status is 401 Unauthorized
```

## Feature: Global error handling

```gherkin
Feature: Global error handling
  As an API consumer
  I want failures to come back as predictable, safe responses
  So that I never see a leaked stack trace or an opaque hang

  Scenario: An unexpected exception does not leak internals
    Given some unhandled exception occurs while processing a request
    When the response is returned to the client
    Then the response status is 500
    And the response body is a generic ProblemDetails payload with a traceId
    And the response body does not contain a stack trace or exception message
    And the full exception, including stack trace, is written to the structured logs

  Scenario: Expected failures are not treated as server errors
    Given a request fails validation, or targets a missing resource, or conflicts with existing data
    When the response is returned to the client
    Then the status code reflects the failure (400, 404, or 409)
    And no exception is logged, because this is expected application behavior, not a bug

  Scenario: Every HTTP request produces one structured log line
    When any request is handled, successfully or not
    Then exactly one JSON log line is emitted summarizing method, path, status code, and elapsed time
```

## Feature: Health and platform readiness

```gherkin
Feature: Health check
  As an operator
  I want a liveness endpoint
  So that orchestrators (Docker Compose, Azure Container Apps) know when the API is ready

  Scenario: Health check while dependencies are up
    Given MongoDB is reachable
    When I GET /health
    Then the response status is 200 OK
    And the body reports "Healthy"
```

## Feature: Startup migrations

```gherkin
Feature: Automatic Mongo migrations
  As an operator
  I want required indexes to exist without a manual step
  So that deploying a fresh environment doesn't require running commands by hand

  Scenario: First boot against an empty database
    Given a brand-new, empty MongoDB database
    When the API starts for the first time
    Then the users and games collections exist before any request is served
    And a unique index exists on users.email
    And indexes exist on games.title and games.genre

  Scenario: Migrations only run once
    Given the API has already started once against a database
    When the API restarts against the same database
    Then already-applied migrations are not re-applied
```
