using System.Windows;
using MiniPdm.Infrastructure.Data;
using MiniPdm.UI.Composition;

namespace MiniPdm.UI;

public partial class App : System.Windows.Application
{
    private AppCompositionRoot? _compositionRoot;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var connectionString = Environment.GetEnvironmentVariable("MINIPDM_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=minipdm;Username=postgres;Password=postgres;";

        var providerStr = Environment.GetEnvironmentVariable("MINIPDM_PROVIDER");
        var provider = string.Equals(providerStr, "SqlServer", StringComparison.OrdinalIgnoreCase)
            ? DatabaseProvider.SqlServer
            : DatabaseProvider.PostgreSql;

        var factory = new DbConnectionFactory(connectionString, provider);

        try
        {
            var initializer = new DatabaseInitializer(factory);
            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось подключиться к базе данных {provider}:\n{ex.Message}\n\nУбедитесь, что СУБД запущена (например: docker compose up -d).",
                "База данных",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _compositionRoot = new AppCompositionRoot(factory);
        var mainWindow = _compositionRoot.GetService<MainWindow>();
        mainWindow.Show();
    }
}
