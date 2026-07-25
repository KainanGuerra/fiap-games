using FiapGames.Modules.Users.Application.Abstractions;
using FiapGames.Modules.Users.Domain;
using FiapGames.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace FiapGames.Modules.Users.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToLowerInvariant();
        return _context.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<PagedResult<User>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.OrderBy(u => u.CreatedAtUtc);

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<User>(items, totalCount, request.Page, request.PageSize);
    }

    public Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(User entity) => _context.Users.Update(entity);

    public void Remove(User entity) => _context.Users.Remove(entity);

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken) >= 0;
}
