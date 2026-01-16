using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenGameVerse.App.ViewModels;
using OpenGameVerse.App.Views;
using OpenGameVerse.App.Services;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Data;
using OpenGameVerse.Data.Repositories;
using OpenGameVerse.Metadata.Abstractions;
using OpenGameVerse.Metadata.Services;
using OpenGameVerse.Platform.Windows;
using OpenGameVerse.Platform.Linux;

namespace OpenGameVerse.App;

public partial class App : Application
{
    private IGameRepository? _gameRepository;
    private ICategoryRepository? _categoryRepository;
    private IPlatformHost? _platformHost;
    private IMetadataService? _metadataService;
    private DialogService? _dialogService;
    private IAppSettingsService? _settingsService;
    private TrayIconService? _trayIconService;
    private WindowBehaviorService? _windowBehaviorService;
    private GameStatusMonitorService? _gameStatusMonitorService;

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
        _categoryRepository = new CategoryRepository(connectionString);

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

        // Dialog service
        _dialogService = new DialogService();

        // App settings
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenGameVerse", "settings.json");

        var startupService = new StartupService();
        _settingsService = new AppSettingsService(settingsPath, startupService);
        _settingsService.Load();
        _settingsService.ApplyThemeFromSettings();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();

            // Set dialog service's main window
            _dialogService?.SetMainWindow(mainWindow);

            _gameStatusMonitorService = new GameStatusMonitorService();
            _gameStatusMonitorService.Start();

            var viewModel = new MainWindowViewModel(
                _gameRepository!,
                _categoryRepository!,
                _platformHost!,
                _dialogService!,
                _settingsService!,
                _gameStatusMonitorService,
                _metadataService);

            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;

            _trayIconService = new TrayIconService(
                mainWindow,
                () => _windowBehaviorService?.RequestExit());
            _trayIconService.UpdateSettings(_settingsService!.CurrentSettings);

            _windowBehaviorService = new WindowBehaviorService(
                mainWindow,
                _settingsService!,
                _trayIconService);

            // Initialize after window is created
            _ = viewModel.InitializeAsync();

            ApplyStartupWindowBehavior(mainWindow, _settingsService.CurrentSettings);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyStartupWindowBehavior(Window mainWindow, OpenGameVerse.Core.Models.AppSettings settings)
    {
        if (settings.StartInFullscreen)
        {
            var fullscreenViewModel = new FullscreenViewModel(
                _gameRepository!,
                _platformHost!,
                mainWindow,
                _settingsService!,
                _gameStatusMonitorService!,
                _metadataService);

            var fullscreenWindow = new FullscreenWindow
            {
                DataContext = fullscreenViewModel
            };

            fullscreenWindow.Closed += (_, _) => mainWindow.Show();
            mainWindow.Hide();
            fullscreenWindow.Show();
            _ = fullscreenViewModel.InitializeAsync();
            return;
        }

        if (settings.StartOnStartup && settings.MinimizeToTrayAfterStartup && settings.ShowTrayIcon)
        {
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Hide();
            return;
        }

        if (settings.StartMinimized)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }
    }
}
