using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Data;
using OpenGameVerse.Data.Repositories;
using OpenGameVerse.Platform.Linux;
using OpenGameVerse.Platform.Windows;

namespace OpenGameVerse.Console.DependencyInjection;

/// <summary>
/// Manual DI container for AOT compatibility
/// No reflection-based assembly scanning
/// </summary>
public sealed class ServiceContainer
{
    private readonly Dictionary<Type, object> _services = new();

    public void RegisterServices()
    {
        // Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenGameVerse",
            "opengameverse.db"
        );

        var connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate";

        // Initialize database
        var dbContext = new DatabaseContext(connectionString);
        dbContext.Initialize();

        Register<IGameRepository>(new GameRepository(connectionString));

        // Platform host
        IPlatformHost platformHost =
            OperatingSystem.IsWindows() ? new WindowsPlatformHost()
            : OperatingSystem.IsLinux() ? new LinuxPlatformHost()
            : throw new PlatformNotSupportedException();
        Register(platformHost);
    }

    public void Register<T>(T instance)
        where T : notnull
    {
        _services[typeof(T)] = instance;
    }

    public T Resolve<T>()
        where T : notnull
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }

        throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }
}
