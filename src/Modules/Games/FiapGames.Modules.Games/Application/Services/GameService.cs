using FiapGames.Modules.Games.Application.Abstractions;
using FiapGames.Modules.Games.Application.Dtos;
using FiapGames.Modules.Games.Domain;
using FiapGames.Shared.Kernel.Pagination;
using FiapGames.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace FiapGames.Modules.Games.Application.Services;

public sealed class GameService : IGameService
{
    private readonly IGameRepository _repository;
    private readonly ILogger<GameService> _logger;

    public GameService(IGameRepository repository, ILogger<GameService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = new Game(request.Title, request.Genre, request.Platform, request.Price, request.ReleaseDate, request.Description);

        await _repository.AddAsync(game, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Game {GameId} created: {Title}", game.Id, game.Title);

        return GameResponse.FromDomain(game);
    }

    public async Task<Result<GameResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _repository.GetByIdAsync(id, cancellationToken);
        if (game is null)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return Result.Failure<GameResponse>(Error.NotFound($"Game '{id}' was not found."));
        }

        return Result.Success(GameResponse.FromDomain(game));
    }

    public async Task<PagedResult<GameResponse>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var paged = await _repository.GetPagedAsync(request, cancellationToken);
        var items = paged.Items.Select(GameResponse.FromDomain).ToList();
        return new PagedResult<GameResponse>(items, paged.TotalCount, paged.Page, paged.PageSize);
    }

    public async Task<Result<GameResponse>> UpdateAsync(Guid id, UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = await _repository.GetByIdAsync(id, cancellationToken);
        if (game is null)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return Result.Failure<GameResponse>(Error.NotFound($"Game '{id}' was not found."));
        }

        game.UpdateDetails(request.Title, request.Genre, request.Platform, request.Price, request.ReleaseDate, request.Description);
        _repository.Update(game);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Game {GameId} updated", game.Id);

        return Result.Success(GameResponse.FromDomain(game));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _repository.GetByIdAsync(id, cancellationToken);
        if (game is null)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return Result.Failure(Error.NotFound($"Game '{id}' was not found."));
        }

        _repository.Remove(game);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Game {GameId} deleted", id);

        return Result.Success();
    }
}
