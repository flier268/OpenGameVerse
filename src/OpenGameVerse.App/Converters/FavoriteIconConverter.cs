using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGameVerse.App.Converters;

public sealed class FavoriteIconConverter : IValueConverter
{
    public static FavoriteIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFavorite)
        {
            return isFavorite ? "★" : "☆";
        }

        return "☆";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
