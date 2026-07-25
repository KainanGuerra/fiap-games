namespace FiapGames.Modules.Games.Application.Dtos;

public sealed record CreateGameRequest(
    string Title,
    string Genre,
    string Platform,
    decimal Price,
    DateOnly ReleaseDate,
    string? Description);

public sealed record UpdateGameRequest(
    string Title,
    string Genre,
    string Platform,
    decimal Price,
    DateOnly ReleaseDate,
    string? Description);

public sealed record GameResponse(
    Guid Id,
    string Title,
    string Genre,
    string Platform,
    decimal Price,
    DateOnly ReleaseDate,
    string? Description,
    DateTime CreatedAtUtc)
{
    public static GameResponse FromDomain(Domain.Game game) => new(
        game.Id,
        game.Title,
        game.Genre,
        game.Platform,
        game.Price,
        game.ReleaseDate,
        game.Description,
        game.CreatedAtUtc);
}
