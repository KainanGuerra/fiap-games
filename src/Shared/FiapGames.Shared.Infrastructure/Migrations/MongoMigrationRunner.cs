using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FiapGames.Shared.Infrastructure.Migrations;

public sealed class MongoMigrationRunner
{
    private const string HistoryCollectionName = "_migrationHistory";

    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoMigrationRunner> _logger;

    public MongoMigrationRunner(IMongoDatabase database, ILogger<MongoMigrationRunner> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task RunAsync(IEnumerable<IMongoMigration> migrations, CancellationToken cancellationToken = default)
    {
        var history = _database.GetCollection<BsonDocument>(HistoryCollectionName);
        var appliedVersions = (await history.Find(FilterDefinition<BsonDocument>.Empty)
                .ToListAsync(cancellationToken))
            .Select(doc => doc["version"].ToInt64())
            .ToHashSet();

        foreach (var migration in migrations.OrderBy(m => m.Version))
        {
            if (appliedVersions.Contains(migration.Version))
                continue;

            _logger.LogInformation("Applying Mongo migration {Version} - {Name}", migration.Version, migration.Name);

            await migration.ExecuteAsync(_database, cancellationToken);

            await history.InsertOneAsync(new BsonDocument
            {
                { "version", migration.Version },
                { "name", migration.Name },
                { "appliedAtUtc", DateTime.UtcNow }
            }, cancellationToken: cancellationToken);
        }
    }
}
