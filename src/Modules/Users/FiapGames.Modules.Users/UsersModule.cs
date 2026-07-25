using FiapGames.Modules.Users.Application.Abstractions;
using FiapGames.Modules.Users.Application.Services;
using FiapGames.Modules.Users.Application.Validators;
using FiapGames.Modules.Users.Endpoints;
using FiapGames.Modules.Users.Infrastructure.Migrations;
using FiapGames.Modules.Users.Infrastructure.Persistence;
using FiapGames.Shared.Infrastructure.Migrations;
using FiapGames.Shared.Infrastructure.Settings;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FiapGames.Modules.Users;

public sealed class UsersModule : Shared.Infrastructure.Modules.IModule
{
    public string Name => "Users";

    public IServiceCollection RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>((sp, options) =>
        {
            var mongoSettings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            options.UseMongoDB(client, mongoSettings.DatabaseName);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();

        services.AddSingleton<IMongoMigration, M0001_CreateUsersIndexes>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUserEndpoints();
        return endpoints;
    }
}
