using System.Globalization;

namespace custom_parser.Formatter;

internal static class ValueFormatter
{
    internal static string Format(object? value, string? format, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
            return formattable.ToString(format, culture);

        if (value is string s)
            return s;

        return value.ToString() ?? string.Empty;
    }
}
