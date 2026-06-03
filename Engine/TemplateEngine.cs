using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using custom_parser.Formatter;
using custom_parser.Lexer;
using custom_parser.Parser;
using custom_parser.Resolver;

namespace custom_parser;

public sealed class TemplateEngine
{
    private readonly ErrorPolicy _errorPolicy;
    private readonly ConcurrentDictionary<string, CompiledTemplate>? _templateCache;

    public TemplateEngine(ErrorPolicy errorPolicy = ErrorPolicy.Throw, bool enableTemplateCache = false)
    {
        _errorPolicy = errorPolicy;
        _templateCache = enableTemplateCache ? new ConcurrentDictionary<string, CompiledTemplate>() : null;
    }

    public string Format(string template, IValueProvider context, CultureInfo? culture = null)
    {
        var compiled = _templateCache is null
            ? Parse(template)
            : _templateCache.GetOrAdd(template, Parse);
        return Render(compiled, context, culture);
    }

    public CompiledTemplate Parse(string template)
    {
        var tokens = TemplateLexer.Tokenize(template);
        var segments = new List<TemplateSegment>(tokens.Count);

        foreach (var token in tokens)
        {
            if (token.Kind == TokenKind.Literal)
            {
                segments.Add(new LiteralSegment(token.Value));
                continue;
            }

            var parsed = ExpressionParser.Parse(token.Value, token.Position);
            var source = "{" + token.Value + "}";
            segments.Add(new PlaceholderSegment(parsed.Expression, parsed.Format, source, token.Position));
        }

        return new CompiledTemplate(segments);
    }

    public string Render(CompiledTemplate compiled, IValueProvider context, CultureInfo? culture = null)
    {
        var cultureInfo = culture ?? CultureInfo.CurrentCulture;
        var builder = new StringBuilder(EstimateCapacity(compiled));

        foreach (var segment in compiled.Segments)
        {
            switch (segment)
            {
                case LiteralSegment literal:
                    builder.Append(literal.Text);
                    break;
                case PlaceholderSegment placeholder:
                    builder.Append(RenderPlaceholder(placeholder, context, cultureInfo));
                    break;
            }
        }

        return builder.ToString();
    }

    private static int EstimateCapacity(CompiledTemplate compiled)
    {
        var capacity = 0;
        foreach (var segment in compiled.Segments)
        {
            if (segment is LiteralSegment literal)
                capacity += literal.Text.Length;
            else
                capacity += 16;
        }

        return capacity;
    }

    private string RenderPlaceholder(PlaceholderSegment placeholder, IValueProvider context, CultureInfo culture)
    {
        var result = ValueResolver.Resolve(placeholder.Expression, context);
        if (result.Outcome == ResolveOutcome.Missing)
            return HandleResolutionError(placeholder);

        return ValueFormatter.Format(result.Value, placeholder.Format, culture);
    }

    private string HandleResolutionError(PlaceholderSegment placeholder) =>
        _errorPolicy switch
        {
            ErrorPolicy.Throw => throw new TemplateRenderException(
                "Failed to resolve placeholder.",
                placeholder.SourceText,
                placeholder.Position),
            ErrorPolicy.Empty => string.Empty,
            ErrorPolicy.KeepPlaceholder => placeholder.SourceText,
            _ => throw new InvalidOperationException($"Unknown error policy: {_errorPolicy}"),
        };
}
