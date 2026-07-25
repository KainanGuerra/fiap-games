using FiapGames.Modules.Games.Application.Abstractions;
using FiapGames.Modules.Games.Application.Dtos;
using FiapGames.Shared.Infrastructure.Extensions;
using FiapGames.Shared.Kernel.Pagination;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FiapGames.Modules.Games.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/games").WithTags("Games").RequireAuthorization();

        group.MapPost("/", async (
            CreateGameRequest request,
            IValidator<CreateGameRequest> validator,
            IGameService service,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var game = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/games/{game.Id}", game);
        });

        group.MapGet("/{id:guid}", async (Guid id, IGameService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetByIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/", async ([AsParameters] PagedRequest request, IGameService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetPagedAsync(request, cancellationToken);
            return Results.Ok(result);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateGameRequest request,
            IValidator<UpdateGameRequest> validator,
            IGameService service,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.UpdateAsync(id, request, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IGameService service, CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result.ToHttpResult();
        });

        return endpoints;
    }
}
