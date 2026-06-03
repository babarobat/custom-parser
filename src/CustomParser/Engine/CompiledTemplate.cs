using CustomParser.Parser;

namespace CustomParser;

internal abstract record TemplateSegment;

internal sealed record LiteralSegment(string Text) : TemplateSegment;

internal sealed record PlaceholderSegment(
    AstNode Expression,
    string? Format,
    string SourceText,
    int Position) : TemplateSegment;

public sealed class CompiledTemplate
{
    internal CompiledTemplate(IReadOnlyList<TemplateSegment> segments) =>
        Segments = segments;

    internal IReadOnlyList<TemplateSegment> Segments { get; }
}
