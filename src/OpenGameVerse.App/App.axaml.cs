using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenGameVerse.App.ViewModels;
using OpenGameVerse.App.Views;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Data;
using OpenGameVerse.Data.Repositories;
using OpenGameVerse.Metadata.Abstractions;
using OpenGameVerse.Metadata.Services;

#if WINDOWS
using OpenGameVerse.Platform.Windows;
#else
using OpenGameVerse.Platform.Linux;
#endif

namespace OpenGameVerse.App;

public partial class App : Application
{
    private IGameRepository? _gameRepository;
    private IPlatformHost? _platformHost;
    private IMetadataService? _metadataService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        InitializeServices();
    }

    private void InitializeServices()
    {
        // Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenGameVerse", "opengameverse.db");

        var connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate";

        // Initialize database
        var dbContext = new DatabaseContext(connectionString);
        dbContext.Initialize();

        _gameRepository = new GameRepository(connectionString);

        // Platform host
#if WINDOWS
        _platformHost = new WindowsPlatformHost();
#else
        _platformHost = new LinuxPlatformHost();
#endif

        // Metadata services
        var clientId = Environment.GetEnvironmentVariable("IGDB_CLIENT_ID") ?? "";
        var clientSecret = Environment.GetEnvironmentVariable("IGDB_CLIENT_SECRET") ?? "";

        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
        {
            var httpClient = new HttpClient();
            var igdbClient = new IgdbClient(httpClient, clientId, clientSecret);

            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenGameVerse", "covers");

            var imageCache = new ImageCache(cacheDir);
            _metadataService = new MetadataService(igdbClient, imageCache);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel(_gameRepository!, _platformHost!, _metadataService);

            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };

            desktop.MainWindow = mainWindow;

            // Initialize after window is created
            _ = viewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}