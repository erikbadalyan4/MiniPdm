using System.Reflection;
using FluentAssertions;
using MiniPdm.Infrastructure.Data;

namespace MiniPdm.Tests.Infrastructure;

public class DatabaseMigratorTests
{
    [Fact]
    public void MigrationScripts_ShouldBeEmbeddedInAssembly()
    {
        var assembly = typeof(DatabaseMigrator).Assembly;
        var names = assembly.GetManifestResourceNames();

        names.Should().Contain(n => n.Contains("postgresql", StringComparison.OrdinalIgnoreCase) && n.EndsWith(".sql"));
        names.Should().Contain(n => n.Contains("sqlserver", StringComparison.OrdinalIgnoreCase) && n.EndsWith(".sql"));
    }

    [Fact]
    public async Task MigrateAsync_AgainstLocalPostgres_ShouldCreateTables()
    {
        const string connStr = "Host=localhost;Port=5433;Database=minipdm;Username=postgres;Password=postgres;";
        var migrator = new DatabaseMigrator(connStr, DatabaseProvider.PostgreSql);

        await migrator.MigrateAsync();
        true.Should().BeTrue();
    }
}
