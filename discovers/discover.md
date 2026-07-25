# Discoveries

*[Leia em Português](discover.pt-BR.md)*

Notes on concepts that came up while building this project and are worth writing down — things that, in the process of explaining the "why" behind an architectural decision, turned out to be older and better-established ideas than they first appeared.

## 1. "Shared Kernel" isn't a .NET feature, it's a term from 2003

It isn't anything specific to modern .NET architecture. It's a pattern from Eric Evans' *Domain-Driven Design* book, **from 2003** — well before .NET Core, Minimal APIs, or anything in the current ecosystem existed.

The idea: when you split your system into independent contexts (in this project's case, `Users` and `Games`), they sometimes need to share *something* without turning into a mess of everything depending on everything. That "something," small and stable, is the *shared kernel*. Here it's just a plain class library project (`FiapGames.Shared.Kernel`), with no dependency on ASP.NET, EF Core, or Mongo — the name "kernel" is just a metaphor for "core," there's no language magic behind it.

## 2. How C# actually turns into a running program

It's not one step, it's two:

1. **Build time**: the compiler (Roslyn) turns the `.cs` into **IL** (Intermediate Language) — a generic bytecode that doesn't yet know which CPU it'll run on. This becomes the `.dll`.
2. **Run time**: the **CLR** (Common Language Runtime) loads that IL, and the **JIT** (Just-In-Time compiler) translates it, method by method, into the actual machine code for whatever CPU it's running on at that moment.

That's why the same `.dll` runs on Windows, Linux, Mac, ARM, or x64 without recompiling — the only thing that changes is that final JIT step.

The CLR also handles: the Garbage Collector (automatic memory management, no manual `free()`), runtime type checking, exceptions, threading.

There's a newer alternative, **Native AOT** (matured starting with .NET 7/8): it compiles straight to native machine code at build time, skipping the JIT at runtime entirely. It gets near-instant startup, but loses some flexibility (arbitrary dynamic reflection, for instance). This project doesn't use it — it runs on the traditional JIT model.

## 3. The .NET timeline (none of this is "new in .NET 10")

- **.NET Framework** (2002–2019): Windows only.
- **.NET Core** (2016): open-source rewrite, cross-platform.
- **.NET 5** (2020): drops the "Core" name, unifies the versioning — there's no longer two separate lines.
- **.NET 6 → 10**: one release a year, every November. **Even numbers are LTS** (3 years of support — the case for the .NET 10 used in this project), **odd numbers are STS** (18 months).

The JIT/CLR has existed since 2002. What changes release to release is performance and new APIs, not the basic concept of how the runtime works.

## 4. "Kernel" is a recycled word, not a single concept

The most common association is "kernel" = the Linux kernel — the real, massive, critical piece of software that runs in privileged mode (**ring 0**) and is the only thing that talks directly to hardware (CPU scheduling, physical memory, drivers, syscalls). A regular program (including the .NET CLR itself) runs in **ring 3**, with no privilege at all, and asks the kernel for everything via a syscall.

But "kernel" in plain English just means "the core of something" — every area of computing (and math) borrowed that word for its own use, with no technical relationship between them:

- **Linux/OS kernel**: the real core of the operating system, ring 0, manages hardware.
- **Shared Kernel (DDD)**: shared code core between bounded contexts — just a project/folder.
- **Jupyter kernel**: the process that actually executes the code behind a notebook's cells.
- **Kernel in Linear Algebra**: the set of vectors a linear transformation maps to zero — purely mathematical.

None of these are implementation-related to each other. It's vocabulary reuse, not conceptual kinship — and this happens constantly in software engineering ("domain," "context," "service" are also plain words hijacked to mean something more specific inside a technical context).

## 5. .NET 6 drastically changed `Program.cs` (and this project uses that new shape)

This one's real, not a wrong impression — .NET 6 (November 2021) was genuinely a turning point for what ASP.NET Core code looks like, especially the app's entry file. Before it, every ASP.NET Core project had **two mandatory files**:

- `Program.cs`: just a `Main` calling `CreateHostBuilder(args).Build().Run()`.
- `Startup.cs`: with two separate methods, `ConfigureServices(IServiceCollection services)` (register DI) and `Configure(IApplicationBuilder app, ...)` (wire up the middleware pipeline).

.NET 6 introduced the **Minimal Hosting Model**: the two files collapse into one, no `Startup` class, no explicit `Main` method, using a language feature called *top-level statements* (which technically already existed since C# 9/.NET 5, but only became ASP.NET Core's default template in .NET 6). This project's `Program.cs` is exactly that shape:

```csharp
var builder = WebApplication.CreateBuilder(args);   // replaces Host.CreateDefaultBuilder + Startup.ConfigureServices
builder.Services.AddMongoDatabase(...);             // DI registration, right here
var app = builder.Build();
app.UseAuthentication();                             // replaces Startup.Configure
app.MapUserEndpoints();
app.Run();
```

Notice there's no visible `class Program { static void Main(string[] args) { ... } }` — the compiler generates that class and that `Main` under the hood from the "loose" code in the file. It's pure syntactic sugar: the generated IL is equivalent to the old explicit `Main`, just without the boilerplate.

Two other changes from the same wave (.NET 6 / C# 10) show up scattered across the project:

- **File-scoped namespaces** (`namespace FiapGames.Modules.Users.Domain;`, no `{ }` wrapping the whole class) — one less indentation level, present in every `.cs` file in the project.
- **`ImplicitUsings`** (visible in the `.csproj` as `<ImplicitUsings>enable</ImplicitUsings>`) — the compiler automatically injects the most common `using` directives (`System`, `System.Linq`, etc.) without needing to write them at the top of every file.

None of this changes what the CLR/JIT actually does (that's stayed the same since 2002, as covered in item 2) — it's purely syntactic sugar on top of C#, decided by the language team, not a runtime change.

## Glossary

- **AOT (Ahead-Of-Time)** — compiling straight to native machine code at build time, without relying on the JIT at runtime.
- **ARM** — a processor architecture (e.g. Apple's M1/M2 chips, a lot of modern cloud servers), an alternative to x86/x64.
- **ASP.NET / ASP.NET Core** — Microsoft's web framework built on top of .NET, used to build APIs and sites (it's what generates this project's `Program.cs`).
- **CLR (Common Language Runtime)** — .NET's runtime: loads the IL, runs the JIT, and handles memory (Garbage Collector), types, exceptions, and threads.
- **CPU** — the processor, the thing that actually executes the machine code the JIT produces.
- **DDD (Domain-Driven Design)** — a software modeling approach centered on the business domain's language and rules.
- **DI (Dependency Injection)** — a pattern where an object receives its dependencies from outside (typically via its constructor) instead of creating them itself.
- **DLL (Dynamic Link Library)** — the binary file produced by the build (e.g. `FiapGames.Api.dll`), containing the compiled IL and its metadata; it's what the CLR loads and the JIT translates at runtime.
- **EF / EF Core (Entity Framework Core)** — Microsoft's official ORM; in this project, it's the layer used to access MongoDB.
- **IL (Intermediate Language)** — the CPU-agnostic intermediate bytecode the C# compiler produces, which becomes the `.dll`.
- **JIT (Just-In-Time compiler)** — the part of the CLR that translates IL into real machine code, method by method, at runtime.
- **LTS (Long-Term Support)** — a .NET release with extended official support (3 years) — the case for the .NET 10 used in this project.
- **STS (Standard-Term Support)** — a .NET release with shorter support (18 months).
- **OS (Operating System)** — the software layer that manages hardware, processes, and memory (e.g. Linux, Windows); it's what runs the kernel mentioned in item 4.
