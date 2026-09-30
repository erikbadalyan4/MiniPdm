using System.Data.Common;
using MiniPdm.Application.Abstractions;

namespace MiniPdm.Infrastructure.Data;

public interface IDbContext : IUnitOfWork, IAsyncDisposable
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
}

public sealed class DbUnitOfWork : IDbContext
{
    private readonly IDbConnectionFactory _connectionFactory;
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    public DbConnection Connection => _connection ??= _connectionFactory.CreateConnection();
    public DbTransaction? Transaction => _transaction;

    public DbUnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (Connection.State != System.Data.ConnectionState.Open)
        {
            await Connection.OpenAsync(ct);
        }

        _transaction = await Connection.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}

