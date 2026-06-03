using custom_parser;
using custom_parser.Parser;

namespace custom_parser.Tests.Helpers;

internal static class ParseTestHelpers
{
    internal sealed record PlaceholderInfo(
        string Root,
        IReadOnlyList<AccessSegment> Segments,
        string? Format,
        string SourceText,
        int Position);

    internal static IReadOnlyList<object> DescribeSegments(CompiledTemplate compiled)
    {
        var result = new List<object>();
        foreach (var segment in compiled.Segments)
        {
            switch (segment)
            {
                case LiteralSegment literal:
                    result.Add(literal.Text);
                    break;
                case PlaceholderSegment placeholder:
                    var path = Assert.IsType<AccessPathNode>(placeholder.Expression);
                    result.Add(new PlaceholderInfo(
                        path.Root,
                        path.Segments,
                        placeholder.Format,
                        placeholder.SourceText,
                        placeholder.Position));
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected segment: {segment.GetType().Name}");
            }
        }

        return result;
    }

    internal static PlaceholderInfo SinglePlaceholder(CompiledTemplate compiled)
    {
        var placeholders = DescribeSegments(compiled).OfType<PlaceholderInfo>().ToList();
        Assert.Single(placeholders);
        return placeholders[0];
    }

    internal static string SingleLiteral(CompiledTemplate compiled)
    {
        var literals = DescribeSegments(compiled).OfType<string>().ToList();
        Assert.Single(literals);
        return literals[0];
    }
}
