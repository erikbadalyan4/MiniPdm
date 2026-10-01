using System.Reflection;
using DbUp;

namespace MiniPdm.Infrastructure.Data;

public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken ct = default);
}

public sealed class DatabaseMigrator : IDatabaseMigrator, IDatabaseInitializer
{
    private readonly string _connectionString;
    private readonly DatabaseProvider _provider;

    public DatabaseMigrator(IDbConnectionFactory connectionFactory)
        : this(connectionFactory.CreateConnection().ConnectionString, connectionFactory.Provider)
    {
    }

    public DatabaseMigrator(string connectionString, DatabaseProvider provider)
    {
        _connectionString = connectionString;
        _provider = provider;
    }

    public Task InitializeAsync(CancellationToken ct = default) => MigrateAsync(ct);

    public async Task MigrateAsync(CancellationToken ct = default)
    {
        const int maxRetries = 5;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                RunDbUpUpgrade();
                return;
            }
            catch when (attempt < maxRetries)
            {
                await Task.Delay(1000, ct);
            }
        }
    }

    private void RunDbUpUpgrade()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var upgraderBuilder = _provider switch
        {
            DatabaseProvider.PostgreSql => DeployChanges.To
                .PostgresqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(assembly, script => script.Contains("postgresql", StringComparison.OrdinalIgnoreCase)),

            DatabaseProvider.SqlServer => DeployChanges.To
                .SqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(assembly, script => script.Contains("sqlserver", StringComparison.OrdinalIgnoreCase)),

            _ => throw new NotSupportedException($"Провайдер {_provider} не поддерживается для миграций.")
        };

        var upgrader = upgraderBuilder
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException($"Ошибка выполнения миграций DbUp: {result.Error?.Message}", result.Error);
        }
    }
}

