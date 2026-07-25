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

## Glossário

- **AOT (Ahead-Of-Time)** — compilação direto pra código de máquina nativo, feita no build, sem depender do JIT em tempo de execução.
- **ARM** — arquitetura de processador (ex.: chips Apple M1/M2, boa parte dos servidores cloud modernos), alternativa ao x86/x64.
- **ASP.NET / ASP.NET Core** — o framework web da Microsoft construído sobre o .NET, usado pra criar APIs e sites (é o que gera o `Program.cs` deste projeto).
- **CLR (Common Language Runtime)** — o runtime do .NET: carrega o IL, executa o JIT, e cuida de memória (Garbage Collector), tipos, exceções e threads.
- **CPU** — o processador, quem de fato executa o código de máquina gerado pelo JIT.
- **DDD (Domain-Driven Design)** — abordagem de modelagem de software focada na linguagem e nas regras do domínio de negócio.
- **DI (Dependency Injection)** — padrão em que um objeto recebe suas dependências de fora (normalmente via construtor) em vez de criá-las internamente.
- **DLL (Dynamic Link Library)** — o arquivo binário resultado do build (ex.: `FiapGames.Api.dll`), contendo o IL compilado e seus metadados; é o que o CLR carrega e o JIT traduz em tempo de execução.
- **EF / EF Core (Entity Framework Core)** — o ORM oficial da Microsoft; neste projeto, é a camada usada pra acessar o MongoDB.
- **IL (Intermediate Language)** — o bytecode intermediário gerado pelo compilador C#, independente de CPU, que vira o `.dll`.
- **JIT (Just-In-Time compiler)** — parte do CLR que traduz o IL pra código de máquina real, método por método, em tempo de execução.
- **LTS (Long-Term Support)** — versão do .NET com suporte oficial estendido (3 anos) — o caso do .NET 10 usado neste projeto.
- **STS (Standard-Term Support)** — versão do .NET com suporte mais curto (18 meses).
- **SO (Sistema Operacional)** — a camada de software que gerencia hardware, processos e memória (ex.: Linux, Windows); é quem roda o kernel citado no item 4.
