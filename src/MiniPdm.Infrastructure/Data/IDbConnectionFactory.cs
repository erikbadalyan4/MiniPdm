using System.Data;
using System.Data.Common;

namespace MiniPdm.Infrastructure.Data;

public interface IDbConnectionFactory
{
    DatabaseProvider Provider { get; }
    ISqlDialect Dialect { get; }
    DbConnection CreateConnection();
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}

