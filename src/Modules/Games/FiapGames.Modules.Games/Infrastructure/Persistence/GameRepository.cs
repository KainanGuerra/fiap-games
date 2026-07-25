using FiapGames.Modules.Games.Application.Abstractions;
using FiapGames.Modules.Games.Domain;
using FiapGames.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace FiapGames.Modules.Games.Infrastructure.Persistence;

public sealed class GameRepository : IGameRepository
{
    private readonly GamesDbContext _context;

    public GameRepository(GamesDbContext context)
    {
        _context = context;
    }

    public Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<PagedResult<Game>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Games.OrderBy(g => g.CreatedAtUtc);

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Game>(items, totalCount, request.Page, request.PageSize);
    }

    public Task AddAsync(Game entity, CancellationToken cancellationToken = default)
    {
        _context.Games.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(Game entity) => _context.Games.Update(entity);

    public void Remove(Game entity) => _context.Games.Remove(entity);

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken) >= 0;
}
