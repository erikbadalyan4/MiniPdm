using System.IO;
using System.Windows;
using MiniPdm.Infrastructure.Data;
using MiniPdm.UI.Composition;
using Serilog;

namespace MiniPdm.UI;

public partial class App : System.Windows.Application
{
    private AppCompositionRoot? _compositionRoot;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logDirectory, "minipdm-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Запуск приложения Мини-PDM");

        var connectionString = Environment.GetEnvironmentVariable("MINIPDM_CONNECTION_STRING")
            ?? "Host=localhost;Port=5433;Database=minipdm;Username=postgres;Password=postgres;";

        var providerStr = Environment.GetEnvironmentVariable("MINIPDM_PROVIDER");
        var provider = string.Equals(providerStr, "SqlServer", StringComparison.OrdinalIgnoreCase)
            ? DatabaseProvider.SqlServer
            : DatabaseProvider.PostgreSql;

        Log.Information("Подключение к СУБД: {Provider}", provider);

        var factory = new DbConnectionFactory(connectionString, provider);

        try
        {
            var initializer = new DatabaseInitializer(factory);
            await initializer.InitializeAsync();
            Log.Information("Инициализация базы данных успешно завершена");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка инициализации базы данных {Provider}", provider);
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

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Завершение работы приложения Мини-PDM");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

