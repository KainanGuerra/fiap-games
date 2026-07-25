using FiapGames.Modules.Games.Application.Abstractions;
using FiapGames.Modules.Games.Application.Services;
using FiapGames.Modules.Games.Application.Validators;
using FiapGames.Modules.Games.Endpoints;
using FiapGames.Modules.Games.Infrastructure.Migrations;
using FiapGames.Modules.Games.Infrastructure.Persistence;
using FiapGames.Shared.Infrastructure.Migrations;
using FiapGames.Shared.Infrastructure.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FiapGames.Modules.Games;

public sealed class GamesModule : Shared.Infrastructure.Modules.IModule
{
    public string Name => "Games";

    public IServiceCollection RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GamesDbContext>((sp, options) =>
        {
            var mongoSettings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            options.UseMongoDB(client, mongoSettings.DatabaseName);
        });

        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IGameService, GameService>();

        services.AddValidatorsFromAssemblyContaining<CreateGameRequestValidator>();

        services.AddSingleton<IMongoMigration, M0001_CreateGamesIndexes>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGameEndpoints();
        return endpoints;
    }
}
