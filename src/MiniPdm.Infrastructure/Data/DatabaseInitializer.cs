namespace MiniPdm.Infrastructure.Data;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDatabaseMigrator _migrator;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _migrator = new DatabaseMigrator(connectionFactory);
    }

    public Task InitializeAsync(CancellationToken ct = default) => _migrator.MigrateAsync(ct);
}

