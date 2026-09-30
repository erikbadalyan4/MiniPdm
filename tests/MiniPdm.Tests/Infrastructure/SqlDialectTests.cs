using FluentAssertions;
using MiniPdm.Infrastructure.Data;

namespace MiniPdm.Tests.Infrastructure;

public class SqlDialectTests
{
    [Fact]
    public void PostgreSqlDialect_ShouldContainWithRecursiveAndValidKeywords()
    {
        var dialect = new PostgreSqlDialect();

        dialect.Provider.Should().Be(DatabaseProvider.PostgreSql);
        dialect.GetInitSchemaSql().Should().Contain("CREATE TABLE IF NOT EXISTS pdm_object");
        dialect.GetRecursiveBomCteSql().Should().Contain("WITH RECURSIVE bom_tree AS");
        dialect.GetFirstLevelComponentsSql().Should().Contain("FROM bom_link bl");
    }

    [Fact]
    public void SqlServerDialect_ShouldContainWithCteAndValidKeywords()
    {
        var dialect = new SqlServerDialect();

        dialect.Provider.Should().Be(DatabaseProvider.SqlServer);
        dialect.GetInitSchemaSql().Should().Contain("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='pdm_object'");
        dialect.GetRecursiveBomCteSql().Should().Contain("WITH bom_tree AS");
        dialect.GetFirstLevelComponentsSql().Should().Contain("FROM bom_link bl");
    }
}
