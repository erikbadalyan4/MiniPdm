using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace MiniPdm.Infrastructure.Data;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly DatabaseProvider _provider;
    private readonly ISqlDialect _dialect;

    public DatabaseProvider Provider => _provider;
    public ISqlDialect Dialect => _dialect;

    public DbConnectionFactory(string connectionString, DatabaseProvider provider = DatabaseProvider.PostgreSql)
    {
        _connectionString = connectionString;
        _provider = provider;
        _dialect = provider switch
        {
            DatabaseProvider.PostgreSql => new PostgreSqlDialect(),
            DatabaseProvider.SqlServer => new SqlServerDialect(),
            _ => throw new NotSupportedException($"Неподдерживаемый провайдер: {provider}")
        };
    }

    public DbConnection CreateConnection()
    {
        return _provider switch
        {
            DatabaseProvider.PostgreSql => new NpgsqlConnection(_connectionString),
            DatabaseProvider.SqlServer => new SqlConnection(_connectionString),
            _ => throw new NotSupportedException($"Неподдерживаемый провайдер: {_provider}")
        };
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = CreateConnection();
        await connection.OpenAsync(ct);
        return connection;
    }
}

