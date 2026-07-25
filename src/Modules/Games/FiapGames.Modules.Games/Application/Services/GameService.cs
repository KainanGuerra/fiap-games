using FiapGames.Modules.Games.Application.Abstractions;
using FiapGames.Modules.Games.Application.Dtos;
using FiapGames.Modules.Games.Domain;
using FiapGames.Shared.Kernel.Pagination;
using FiapGames.Shared.Kernel.Results;

namespace FiapGames.Modules.Games.Application.Services;

public sealed class GameService : IGameService
{
    private readonly IGameRepository _repository;

    public GameService(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = new Game(request.Title, request.Genre, request.Platform, request.Price, request.ReleaseDate, request.Description);

        await _repository.AddAsync(game, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return GameResponse.FromDomain(game);
    }

    public async Task<Result<GameResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _repository.GetByIdAsync(id, cancellationToken);
        return game is null
            ? Result.Failure<GameResponse>(Error.NotFound($"Game '{id}' was not found."))
            : Result.Success(GameResponse.FromDomain(game));
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
            return Result.Failure<GameResponse>(Error.NotFound($"Game '{id}' was not found."));

        game.UpdateDetails(request.Title, request.Genre, request.Platform, request.Price, request.ReleaseDate, request.Description);
        _repository.Update(game);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(GameResponse.FromDomain(game));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _repository.GetByIdAsync(id, cancellationToken);
        if (game is null)
            return Result.Failure(Error.NotFound($"Game '{id}' was not found."));

        _repository.Remove(game);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
