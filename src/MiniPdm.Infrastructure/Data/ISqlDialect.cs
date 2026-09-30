namespace MiniPdm.Infrastructure.Data;

public interface ISqlDialect
{
    DatabaseProvider Provider { get; }
    string GetInitSchemaSql();
    string GetRecursiveBomCteSql();
    string GetFirstLevelComponentsSql();
}
