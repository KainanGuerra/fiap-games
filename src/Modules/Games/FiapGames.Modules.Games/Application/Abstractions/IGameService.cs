using FiapGames.Modules.Games.Application.Dtos;
using FiapGames.Shared.Kernel.Pagination;
using FiapGames.Shared.Kernel.Results;

namespace FiapGames.Modules.Games.Application.Abstractions;

public interface IGameService
{
    Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default);

    Task<Result<GameResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<GameResponse>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<Result<GameResponse>> UpdateAsync(Guid id, UpdateGameRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
