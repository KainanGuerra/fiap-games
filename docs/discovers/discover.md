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

## 6. Having Terraform in the repo doesn't mean anything is actually deployed

This is a common confusion for anyone seeing IaC for the first time: `infra/terraform/` in this project is a complete, working set of `.tf` files — a Resource Group, a Container Apps Environment, Cosmos DB with the Mongo API, the Container App with every env var the API expects. But **writing that definition down doesn't make it exist in the cloud**. It's the same logic as a `docker-compose.yml`: the file describes what should come up, but nothing comes up until someone actually runs the command.

This project's CI/CD (`.github/workflows/ci-cd.yml`) **stops at GHCR** — it builds and publishes the API image, and that's it. There's no mention of `terraform` or `az` anywhere in the workflow. So today, the only way any of that Terraform becomes real infrastructure on Azure is for someone to manually run `terraform init` / `plan` / `apply` on their own machine, after an `az login`.

That's not an oversight, it's a deliberate choice: **CI** (continuous integration — building, testing, packaging) and **CD** (continuous delivery/deployment — actually applying that to some environment) get lumped together as "CI/CD" but don't have to be automated to the same degree. It makes sense to gate the "D" behind an explicit human decision when the "D" costs real money (`terraform apply` creates real, billed cloud resources) — unlike publishing an image to GHCR, which is essentially free.

Closing that loop end-to-end (code → image → applied infrastructure) would need a third job in the workflow — something like installing Terraform on the runner, authenticating via an Azure Service Principal (stored as a GitHub secret), and running `terraform apply -auto-approve` pointing `container_image` at the freshly published tag. That's a possible "next step," not something that exists today.

## 7. What each `.tf` file in `infra/terraform/` is for

First surprise here: Terraform **doesn't require** this split into multiple files. It reads and merges *every* `.tf` file in a folder as if it were one — you could dump everything into one giant `main.tf` and it'd work exactly the same. Splitting by name is purely a community convention to keep the folder readable, not a technical rule enforced by the tool. That said, this is the convention this project follows:

- **`providers.tf`**: the project's "header" — the minimum Terraform version required, which provider is used (`azurerm`, version `~> 4.0`), and where the *state* (the file that tracks what's already been created) is stored — here, `backend "local"`, meaning a `terraform.tfstate` file on whoever's machine runs `apply`.
- **`variables.tf`**: the "input parameters" — each `variable` declares a name, type, description, and optionally a default value or the `sensitive = true` flag (so Terraform never prints that value in output, as with `jwt_secret` and `container_registry_password`). Nothing here creates any resource, it's just the list of what can be configured from outside.
- **`main.tf`**: where the actual resources are declared — each `resource "azurerm_X" "name"` block is a concrete request of "create this in Azure." In this project: the resource group, the Log Analytics workspace, the Container Apps environment, the Cosmos DB account (with the Mongo API enabled via `capabilities`), the Mongo database inside it, and the Container App with the `secret`/`registry`/`ingress`/`template` blocks that actually run the API image.
- **`outputs.tf`**: the "return value" after `apply` — things you want to read back in the terminal (or feed elsewhere), like the API's public URL (`container_app_fqdn`) or the Cosmos connection string (also marked `sensitive`).
- **`terraform.tfvars.example`**: a sample values file for the `variables.tf` inputs — you copy it to `terraform.tfvars` (which is gitignored) and fill in real values before running `apply`.
- **`.terraform.lock.hcl`**: Terraform's lock file, similar in spirit to a Node `package-lock.json` — it pins the exact version (and hash) of the `azurerm` provider that was downloaded, guaranteeing anyone running `terraform init` pulls the same version, no surprises.

## Glossary

- **AOT (Ahead-Of-Time)** — compiling straight to native machine code at build time, without relying on the JIT at runtime.
- **ARM** — a processor architecture (e.g. Apple's M1/M2 chips, a lot of modern cloud servers), an alternative to x86/x64.
- **ASP.NET / ASP.NET Core** — Microsoft's web framework built on top of .NET, used to build APIs and sites (it's what generates this project's `Program.cs`).
- **CD (Continuous Delivery/Deployment)** — the part of "CI/CD" that actually delivers/applies what was built to some environment; in this project it stops at publishing the image to GHCR and never gets to applying the Terraform.
- **CLR (Common Language Runtime)** — .NET's runtime: loads the IL, runs the JIT, and handles memory (Garbage Collector), types, exceptions, and threads.
- **CPU** — the processor, the thing that actually executes the machine code the JIT produces.
- **DDD (Domain-Driven Design)** — a software modeling approach centered on the business domain's language and rules.
- **DI (Dependency Injection)** — a pattern where an object receives its dependencies from outside (typically via its constructor) instead of creating them itself.
- **DLL (Dynamic Link Library)** — the binary file produced by the build (e.g. `FiapGames.Api.dll`), containing the compiled IL and its metadata; it's what the CLR loads and the JIT translates at runtime.
- **EF / EF Core (Entity Framework Core)** — Microsoft's official ORM; in this project, it's the layer used to access MongoDB.
- **HCL (HashiCorp Configuration Language)** — the language `.tf` files are written in; it's declarative (you describe the desired end state, not the step-by-step to get there).
- **IaC (Infrastructure as Code)** — describing cloud infrastructure in versioned configuration files (here, the `.tf` files in `infra/terraform/`) instead of clicking around a portal; writing the file doesn't apply anything by itself, the tool still has to be run.
- **IL (Intermediate Language)** — the CPU-agnostic intermediate bytecode the C# compiler produces, which becomes the `.dll`.
- **JIT (Just-In-Time compiler)** — the part of the CLR that translates IL into real machine code, method by method, at runtime.
- **LTS (Long-Term Support)** — a .NET release with extended official support (3 years) — the case for the .NET 10 used in this project.
- **STS (Standard-Term Support)** — a .NET release with shorter support (18 months).
- **OS (Operating System)** — the software layer that manages hardware, processes, and memory (e.g. Linux, Windows); it's what runs the kernel mentioned in item 4.
