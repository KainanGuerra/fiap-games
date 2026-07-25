using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapGames.Shared.Infrastructure.Modules;

/// <summary>
/// Contract every bounded-context module implements so the API host can
/// register it without knowing its internals (keeps the monolith modular).
/// </summary>
public interface IModule
{
    string Name { get; }

    IServiceCollection RegisterModule(IServiceCollection services, IConfiguration configuration);

    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
