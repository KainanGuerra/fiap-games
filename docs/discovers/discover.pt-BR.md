# Descobertas

*[Read in English](discover.md)*

Notas sobre conceitos que apareceram durante o desenvolvimento desse projeto e que valem registrar — coisas que, ao explicar o "porquê" das decisões de arquitetura, acabam revelando ideias mais antigas e mais fundamentadas do que parecem à primeira vista.

## 1. "Shared Kernel" não é feature do .NET, é um termo de 2003

Não é nada específico de arquitetura .NET moderna. É um padrão do livro *Domain-Driven Design*, do Eric Evans, **de 2003** — bem antes de existir .NET Core, Minimal APIs, ou qualquer coisa do ecossistema atual.

A ideia: quando você separa seu sistema em contextos independentes (no caso deste projeto, `Users` e `Games`), às vezes eles precisam compartilhar *alguma coisa* sem virar uma bagunça de tudo dependendo de tudo. Esse "alguma coisa", pequeno e estável, é o *shared kernel*. Aqui isso é só um projeto de classe comum (`FiapGames.Shared.Kernel`), sem nenhuma dependência de ASP.NET, EF Core ou Mongo — o nome "kernel" é só uma metáfora de "núcleo", não tem nenhuma mágica de linguagem por trás.

## 2. Como o C# vira, de fato, um programa rodando

Não é uma etapa só, são duas:

1. **Build time**: o compilador (Roslyn) transforma o `.cs` em **IL** (Intermediate Language) — um bytecode genérico, que não sabe ainda em qual CPU vai rodar. Isso vira o `.dll`.
2. **Run time**: o **CLR** (Common Language Runtime) carrega esse IL e o **JIT** (Just-In-Time compiler) traduz, método por método, esse IL pro código de máquina real da CPU onde está rodando naquele momento.

É por isso que o mesmo `.dll` roda em Windows, Linux, Mac, ARM ou x64 sem recompilar — o que muda é só essa etapa final do JIT.

O CLR também cuida de: Garbage Collector (memória automática, sem `free()` manual), verificação de tipos em runtime, exceções, threading.

Existe uma alternativa mais nova, o **Native AOT** (amadurecido a partir do .NET 7/8): compila direto pra código de máquina no build, sem depender do JIT em runtime. Ganha startup quase instantâneo, mas perde flexibilidade (reflection dinâmica, por exemplo). Este projeto não usa isso — roda no modelo JIT tradicional.

## 3. Linha do tempo do .NET (nada disso é "novidade do .NET 10")

- **.NET Framework** (2002–2019): só Windows.
- **.NET Core** (2016): reescrita open source, multiplataforma.
- **.NET 5** (2020): dropa o "Core", unifica a numeração — não existe mais duas linhas separadas.
- **.NET 6 → 10**: um release por ano, sempre em novembro. Números **pares são LTS** (3 anos de suporte — o caso do .NET 10 usado neste projeto), **ímpares são STS** (18 meses).

O JIT/CLR existe desde 2002. O que muda de versão em versão é performance e novas APIs, não o conceito básico de como o runtime funciona.

## 4. "Kernel" é uma palavra reciclada, não um conceito único

A associação mais comum é "kernel" = kernel do Linux — a peça de software real, gigantesca e crítica que roda em modo privilegiado (**ring 0**) e é a única coisa que fala diretamente com o hardware (CPU scheduling, memória física, drivers, syscalls). Um programa comum (incluindo o próprio CLR do .NET) roda em **ring 3**, sem privilégio nenhum, e pede tudo pro kernel via syscall.

Só que "kernel" em inglês é só a palavra pra "núcleo/miolo de algo" — cada área da computação (e da matemática) pegou essa palavra emprestada pro seu próprio uso, sem nenhuma relação técnica entre si:

- **Kernel do Linux/SO**: o núcleo real do sistema operacional, ring 0, gerencia hardware.
- **Shared Kernel (DDD)**: núcleo de código compartilhado entre bounded contexts — é só um projeto/pasta.
- **Kernel do Jupyter**: o processo que executa o código por trás das células de um notebook.
- **Kernel em Álgebra Linear**: o conjunto de vetores que uma transformação linear zera — puramente matemático.

Nenhum desses tem relação de implementação com os outros. É reaproveitamento de vocabulário, não parentesco de conceito — e isso se repete o tempo todo em engenharia de software ("domain", "context", "service" também são palavras comuns sequestradas pra significar algo mais específico dentro de um contexto técnico).

## 5. O .NET 6 mudou drasticamente o `Program.cs` (e o projeto usa esse formato novo)

Essa é real, não é impressão errada — o .NET 6 (novembro de 2021) foi de fato um divisor de águas na cara do código ASP.NET Core, especialmente no arquivo de entrada da aplicação. Antes dele, todo projeto ASP.NET Core tinha **dois arquivos obrigatórios**:

- `Program.cs`: só um `Main` chamando `CreateHostBuilder(args).Build().Run()`.
- `Startup.cs`: com dois métodos separados, `ConfigureServices(IServiceCollection services)` (registrar DI) e `Configure(IApplicationBuilder app, ...)` (montar o pipeline de middleware).

O .NET 6 introduziu o **Minimal Hosting Model**: os dois arquivos viram um só, sem classe `Startup`, sem método `Main` explícito, usando um recurso de linguagem chamado *top-level statements* (na verdade já existia desde o C# 9/.NET 5, mas só virou o template padrão do ASP.NET Core no .NET 6). O `Program.cs` deste projeto é exatamente esse formato:

```csharp
var builder = WebApplication.CreateBuilder(args);   // substitui Host.CreateDefaultBuilder + Startup.ConfigureServices
builder.Services.AddMongoDatabase(...);             // registro de DI, direto aqui
var app = builder.Build();
app.UseAuthentication();                             // substitui Startup.Configure
app.MapUserEndpoints();
app.Run();
```

Repare que não existe `class Program { static void Main(string[] args) { ... } }` visível — o compilador gera essa classe e esse `Main` por baixo dos panos a partir do código "solto" no arquivo. É só açúcar sintático: o IL gerado é equivalente ao `Main` explícito de antes, só que sem o boilerplate.

Duas outras mudanças da mesma leva (.NET 6 / C# 10) aparecem espalhadas pelo projeto:

- **Namespaces com escopo de arquivo** (`namespace FiapGames.Modules.Users.Domain;`, sem chaves `{ }` envolvendo a classe inteira) — um nível de indentação a menos, presente em todo arquivo `.cs` do projeto.
- **`ImplicitUsings`** (visível no `.csproj` como `<ImplicitUsings>enable</ImplicitUsings>`) — o compilador injeta automaticamente os `using` mais comuns (`System`, `System.Linq`, etc.) sem precisar escrever no topo de cada arquivo.

Nada disso muda o que o CLR/JIT faz (isso continua igual desde 2002, como vimos no item 2) — é só açúcar sintático em cima do C#, decidido pelo time de linguagem, não uma mudança de runtime.

## 6. Ter Terraform no repositório não significa que existe deploy acontecendo

Essa confusão é bem comum pra quem está vendo IaC pela primeira vez: `infra/terraform/` neste projeto é um conjunto de arquivos `.tf` completo e funcional — Resource Group, Container Apps Environment, Cosmos DB com API Mongo, o Container App com todas as env vars que a API espera. Mas **ter essa definição escrita não faz ela existir na nuvem**. É a mesma lógica de ter um `docker-compose.yml`: o arquivo descreve o que deveria subir, só que ninguém sobe nada até alguém rodar o comando.

O CI/CD deste projeto (`.github/workflows/ci-cd.yml`) **para no GHCR** — ele builda e publica a imagem da API, e é só isso. Não existe nenhuma menção a `terraform` ou `az` no workflow. Ou seja: hoje, o único jeito de qualquer coisa desse Terraform virar infraestrutura real no Azure é alguém rodar manualmente `terraform init` / `plan` / `apply` na própria máquina, com `az login` feito antes.

Isso não é um esquecimento, é uma decisão deliberada: **CI** (integração contínua — compilar, testar, empacotar) e **CD** (entrega/deploy contínuo — aplicar isso em algum ambiente) são duas coisas frequentemente faladas juntas ("CI/CD") mas que não precisam andar automatizadas na mesma medida. Faz sentido travar o "D" atrás de uma decisão humana explícita quando o "D" custa dinheiro de verdade (`terraform apply` cria recursos reais e cobrados na nuvem) — diferente de publicar uma imagem no GHCR, que é praticamente de graça.

Pra fechar esse ciclo de ponta a ponta (código → imagem → infraestrutura aplicada), faltaria um terceiro job no workflow, algo como instalar o Terraform no runner, autenticar via um Service Principal do Azure (guardado como secret do GitHub), e rodar `terraform apply -auto-approve` apontando o `container_image` pra tag recém-publicada. Isso é só um "próximo passo" possível, não algo que existe hoje.

## 7. Pra que serve cada arquivo `.tf` dentro de `infra/terraform/`

O primeiro estranhamento aqui: o Terraform **não exige** essa divisão em vários arquivos. Ele lê e junta *todos* os `.tf` de uma pasta como se fosse um arquivo único — dá pra jogar tudo num `main.tf` gigante que funciona igual. A separação por nome é só uma convenção da comunidade pra deixar a pasta legível, não uma regra técnica da ferramenta. Dito isso, é essa convenção que este projeto segue:

- **`providers.tf`**: o "cabeçalho" do projeto — qual versão mínima do Terraform é exigida, qual provider é usado (`azurerm`, versão `~> 4.0`) e onde o *state* (o arquivo que registra o que já foi criado) é guardado — aqui, `backend "local"`, ou seja, um arquivo `terraform.tfstate` na própria máquina de quem aplica.
- **`variables.tf`**: os "parâmetros de entrada" — cada `variable` declara um nome, tipo, descrição e, opcionalmente, um valor padrão ou a flag `sensitive = true` (pra Terraform nunca imprimir aquele valor no output, caso de `jwt_secret` e `container_registry_password`). Nada aqui cria recurso nenhum, é só a lista do que pode ser configurado de fora.
- **`main.tf`**: onde os recursos de verdade são declarados — cada bloco `resource "azurerm_X" "nome"` é um pedido concreto de "crie isso no Azure". Neste projeto: resource group, workspace de Log Analytics, o ambiente de Container Apps, a conta Cosmos DB (com a API do Mongo habilitada via `capabilities`), o banco Mongo dentro dela, e o Container App com os `secret`/`registry`/`ingress`/`template` que realmente rodam a imagem da API.
- **`outputs.tf`**: o "retorno" depois do `apply` — valores que você quer poder ler de volta no terminal (ou usar em outro lugar), tipo a URL pública da API (`container_app_fqdn`) ou a connection string do Cosmos (também marcada `sensitive`).
- **`terraform.tfvars.example`**: um exemplo de arquivo de valores pra preencher as `variables.tf` — você copia pra `terraform.tfvars` (esse sim ignorado pelo git) e substitui pelos valores reais antes de rodar `apply`.
- **`.terraform.lock.hcl`**: o "lock file" do Terraform, igual em espírito a um `package-lock.json` do Node — trava a versão exata (e o hash) do provider `azurerm` que foi baixado, garantindo que todo mundo que rodar `terraform init` puxe a mesma versão, sem surpresa.

## 8. O princípio do SOLID por trás da pasta `Shared` é o DIP, não o SRP

Fácil de achar que "separar código compartilhado numa pasta" é só organização (SRP). Mas o motivo real de `Shared.Infrastructure` existir como está — abstrações (`ITokenService`, `IPasswordHasher`) e implementações concretas (`JwtTokenService`, `BCryptPasswordHasher`) morando juntas, mas os módulos (`Users`, `Games`) só enxergando a interface — é o **DIP** (Dependency Inversion Principle): módulos de alto nível não devem depender de implementação concreta de baixo nível, ambos devem depender de abstração. `UserService` nunca faz `new JwtTokenService()`; ele recebe `ITokenService` pronto, injetado de fora. O **ISP** (interfaces pequenas e específicas, uma por responsabilidade) ajuda a viabilizar isso, mas é coadjuvante — quem responde "por que essa pasta existe assim" é o DIP.

## 9. Os design patterns que realmente aparecem no projeto (não é lista de curso)

Vale separar GoF clássico de convenção do próprio .NET:

- **Repository** — `IUserRepository`/`IGameRepository` isolam o acesso a dado do resto do código.
- **Factory Method** — `UserResponse.FromDomain(user)` / `GameResponse.FromDomain(game)`, fábricas estáticas que convertem entidade → DTO num único lugar.
- **Strategy** — `ITokenService`/`IPasswordHasher`: implementação concreta trocável sem tocar em quem consome.
- **Command** — cada `IMongoMigration` encapsula uma ação (`ExecuteAsync`) executada depois, em ordem, pelo `MongoMigrationRunner`.
- **Chain of Responsibility** — o próprio pipeline de middleware do ASP.NET Core (`UseExceptionHandler` → `UseSerilogRequestLogging` → `UseAuthentication` → `UseAuthorization`).
- **Result Object** — `Result`/`Result<T>` como retorno de valor pra falha esperada, no lugar de lançar exceção pra fluxo de controle normal.
- **Options Pattern** — `IOptions<JwtSettings>`/`IOptions<MongoSettings>`, configuração bindada do `appsettings.json`.
- **Extension methods como builder fluente** — `AddJwtAuthentication()`, `AddMongoDatabase()`, `ToHttpResult()`.
- **`IModule`** — não é GoF nomeado, mas funciona como um padrão de auto-registro/plugin: cada módulo se registra no host sem o host conhecer os detalhes internos dele.

## 10. Declarar a interface no construtor não basta — precisa de registro explícito

Declarar `ITokenService` no construtor do `UserService` só diz "preciso de algo que implemente isso" — não faz o container saber *qual* implementação usar. Isso exige um **registro explícito**, em `AuthenticationExtensions.cs`:
```csharp
services.AddSingleton<ITokenService, JwtTokenService>();
```
Sem esse registro, o app **compila normalmente** e só quebra em runtime, na primeira ativação de `UserService`, com `InvalidOperationException: Unable to resolve service for type 'ITokenService'`. O C# não tem como saber disso em tempo de compilação — resolução de tipo por DI é 100% runtime.

## 11. Os três "lifetimes" do DI: Transient, Scoped e Singleton

O **lifetime** escolhido no registro (segundo argumento genérico de `Add___`) controla quanto tempo uma instância é reaproveitada:
- **Transient**: uma instância nova a cada injeção.
- **Scoped**: uma instância por requisição HTTP — é o padrão do `AddDbContext<GamesDbContext>()` (`ServiceLifetime contextLifetime = Scoped`, confirmado via reflection direto na assembly do EF Core, não é suposição). É por isso que `GameRepository`/`GameService` também são `AddScoped` no `GamesModule.cs` — eles dependem do `GamesDbContext`.
- **Singleton**: uma instância pra aplicação inteira — o caso de `JwtTokenService`/`BCryptPasswordHasher`/`IMongoMigration`, que não guardam estado por requisição.

## 12. A pegadinha do "captive dependency" não é simétrica entre os três lifetimes

Um **Singleton nunca pode depender de um Scoped** — Singleton é sempre construído a partir do provider raiz, fora de qualquer requisição, então não existe um "escopo" pra buscar a dependência Scoped, e o container quebra em runtime com "Cannot consume scoped service from singleton". Já um **Transient que depende de um Scoped funciona normalmente**, porque um Transient não tem escopo próprio — ele é resolvido dentro do escopo de quem o pediu, e no ASP.NET Core toda requisição HTTP já roda dentro do seu próprio escopo automaticamente. Um teste rápido com `ServiceProviderOptions { ValidateScopes = true }` confirma isso lado a lado: o mesmo Transient resolvido dentro de um escopo funciona sem erro; resolvido direto do provider raiz (fora de qualquer escopo), quebra com a mesma classe de erro do Singleton. A regra de compatibilidade real é: Singleton só depende com segurança de Singleton; Scoped depende de Scoped ou Singleton; Transient depende de qualquer um dos três, porque ele empresta o escopo de quem o consome em vez de ter um fixo.

## Glossário

- **AOT (Ahead-Of-Time)** — compilação direto pra código de máquina nativo, feita no build, sem depender do JIT em tempo de execução.
- **ARM** — arquitetura de processador (ex.: chips Apple M1/M2, boa parte dos servidores cloud modernos), alternativa ao x86/x64.
- **ASP.NET / ASP.NET Core** — o framework web da Microsoft construído sobre o .NET, usado pra criar APIs e sites (é o que gera o `Program.cs` deste projeto).
- **CD (Continuous Delivery/Deployment)** — a parte de "CI/CD" que efetivamente entrega/aplica o que foi construído em algum ambiente; neste projeto, ela para na publicação da imagem no GHCR e não chega a aplicar o Terraform.
- **CLR (Common Language Runtime)** — o runtime do .NET: carrega o IL, executa o JIT, e cuida de memória (Garbage Collector), tipos, exceções e threads.
- **CPU** — o processador, quem de fato executa o código de máquina gerado pelo JIT.
- **DDD (Domain-Driven Design)** — abordagem de modelagem de software focada na linguagem e nas regras do domínio de negócio.
- **DI (Dependency Injection)** — padrão em que um objeto recebe suas dependências de fora (normalmente via construtor) em vez de criá-las internamente.
- **DLL (Dynamic Link Library)** — o arquivo binário resultado do build (ex.: `FiapGames.Api.dll`), contendo o IL compilado e seus metadados; é o que o CLR carrega e o JIT traduz em tempo de execução.
- **DTO (Data Transfer Object)** — objeto usado só pra transportar dado entre camadas (ex.: `UserResponse`, `GameResponse`), sem comportamento de domínio, diferente da entidade.
- **EF / EF Core (Entity Framework Core)** — o ORM oficial da Microsoft; neste projeto, é a camada usada pra acessar o MongoDB.
- **GoF (Gang of Four)** — apelido dos quatro autores do livro *Design Patterns* (1994), que catalogou padrões como Repository, Factory Method, Strategy, Command e Chain of Responsibility — a referência clássica quando alguém fala "design pattern" sem mais contexto.
- **HCL (HashiCorp Configuration Language)** — a linguagem em que os arquivos `.tf` são escritos; é declarativa (você descreve o resultado desejado, não o passo a passo pra chegar lá).
- **IaC (Infrastructure as Code)** — descrever infraestrutura de nuvem em arquivos de configuração versionados (aqui, os `.tf` em `infra/terraform/`) em vez de clicar manualmente no portal; escrever o arquivo não aplica nada sozinho, é preciso rodar a ferramenta.
- **IL (Intermediate Language)** — o bytecode intermediário gerado pelo compilador C#, independente de CPU, que vira o `.dll`.
- **JIT (Just-In-Time compiler)** — parte do CLR que traduz o IL pra código de máquina real, método por método, em tempo de execução.
- **LTS (Long-Term Support)** — versão do .NET com suporte oficial estendido (3 anos) — o caso do .NET 10 usado neste projeto.
- **STS (Standard-Term Support)** — versão do .NET com suporte mais curto (18 meses).
- **SO (Sistema Operacional)** — a camada de software que gerencia hardware, processos e memória (ex.: Linux, Windows); é quem roda o kernel citado no item 4.
