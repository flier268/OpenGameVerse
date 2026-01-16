using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace OpenGameVerse.App.Converters;

public sealed class FilePathToBitmapConverter : IValueConverter
{
    public static FilePathToBitmapConverter Instance { get; } = new();

    private readonly Dictionary<string, Bitmap> _cache = new(StringComparer.Ordinal);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (_cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var bitmap = new Bitmap(path);
            _cache[path] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
