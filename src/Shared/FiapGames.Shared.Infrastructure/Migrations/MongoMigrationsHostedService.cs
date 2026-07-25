using Microsoft.Extensions.Hosting;

namespace FiapGames.Shared.Infrastructure.Migrations;

/// <summary>
/// Runs every registered <see cref="IMongoMigration"/> once, in version order,
/// before the API starts accepting traffic.
/// </summary>
public sealed class MongoMigrationsHostedService : IHostedService
{
    private readonly MongoMigrationRunner _runner;
    private readonly IEnumerable<IMongoMigration> _migrations;

    public MongoMigrationsHostedService(MongoMigrationRunner runner, IEnumerable<IMongoMigration> migrations)
    {
        _runner = runner;
        _migrations = migrations;
    }

    public Task StartAsync(CancellationToken cancellationToken) => _runner.RunAsync(_migrations, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
