using Dapper;

namespace MiniPdm.Infrastructure.Data;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        const int maxRetries = 5;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
                var sql = _connectionFactory.Dialect.GetInitSchemaSql();
                await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
                return;
            }
            catch when (attempt < maxRetries)
            {
                await Task.Delay(1000, ct);
            }
        }
    }
}
