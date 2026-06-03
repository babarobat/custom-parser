using System.Globalization;
using custom_parser.Resolver;

namespace custom_parser;

public sealed class TemplateEngine
{
    public string Format(string template, IValueProvider context, CultureInfo? culture = null)
        => template; // TODO Parse+Render
}
