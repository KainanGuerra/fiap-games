using System.Security.Claims;
using FiapGames.Modules.Users.Application.Abstractions;
using FiapGames.Modules.Users.Application.Dtos;
using FiapGames.Shared.Infrastructure.Extensions;
using FiapGames.Shared.Kernel.Pagination;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FiapGames.Modules.Users.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").WithTags("Users");

        group.MapPost("/register", async (
            RegisterUserRequest request,
            IValidator<RegisterUserRequest> validator,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.RegisterAsync(request, cancellationToken);
            return result.ToHttpResult(user => Results.Created($"/api/users/{user.Id}", user));
        }).AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.LoginAsync(request, cancellationToken);
            return result.ToHttpResult();
        }).AllowAnonymous();

        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            return Results.Ok(new { id, email = user.FindFirstValue(ClaimTypes.Email) });
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IUserService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetByIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        group.MapGet("/", async ([AsParameters] PagedRequest request, IUserService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetPagedAsync(request, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            IValidator<UpdateUserRequest> validator,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.UpdateAsync(id, request, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, IUserService service, CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        return endpoints;
    }
}
