using custom_parser;
using custom_parser.Parser;

namespace custom_parser.Tests.Parsing;

public class ExpressionParserTests
{
    private const int Pos = 0;

    private static AccessPathNode Path(ParsedPlaceholder parsed) =>
        Assert.IsType<AccessPathNode>(parsed.Expression);

    [Theory]
    [InlineData("name", "name", null)]
    [InlineData("player_health", "player_health", null)]
    [InlineData("kill_count", "kill_count", null)]
    [InlineData("_private", "_private", null)]
    [InlineData("A1_b2", "A1_b2", null)]
    public void Parse_RootOnly_NoSegments(string content, string root, string? format)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        Assert.Equal(root, Path(parsed).Root);
        Assert.Empty(Path(parsed).Segments);
        Assert.Equal(format, parsed.Format);
    }

    [Theory]
    [InlineData("player.health", "player", "health")]
    [InlineData("player.stats.health", "player", "stats", "health")]
    [InlineData("a.b.c.d", "a", "b", "c", "d")]
    public void Parse_MemberChain_BuildsMemberSegments(string content, string root, params string[] members)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        Assert.Equal(root, Path(parsed).Root);
        Assert.Equal(members.Length, Path(parsed).Segments.Count);
        for (var i = 0; i < members.Length; i++)
        {
            var segment = Assert.IsType<MemberSegment>(Path(parsed).Segments[i]);
            Assert.Equal(members[i], segment.Name);
        }
    }

    [Theory]
    [InlineData("weapons[0]", "weapons", 0)]
    [InlineData("items[42]", "items", 42)]
    [InlineData("arr[0][1]", "arr", 0, 1)]
    public void Parse_IndexSegments_ParsesNonNegativeInts(
        string content,
        string root,
        params int[] indices)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        Assert.Equal(root, Path(parsed).Root);
        Assert.All(Path(parsed).Segments, s => Assert.IsType<IndexSegment>(s));
        Assert.Equal(indices, Path(parsed).Segments.Cast<IndexSegment>().Select(s => s.Index).ToArray());
    }

    [Fact]
    public void Parse_MixedChain_MemberAndIndexSegments()
    {
        var parsed = ExpressionParser.Parse("items[0].stats[1].name", Pos);
        Assert.Equal("items", Path(parsed).Root);
        Assert.Equal(4, Path(parsed).Segments.Count);
        Assert.IsType<IndexSegment>(Path(parsed).Segments[0]);
        Assert.Equal(0, ((IndexSegment)Path(parsed).Segments[0]).Index);
        Assert.IsType<MemberSegment>(Path(parsed).Segments[1]);
        Assert.Equal("stats", ((MemberSegment)Path(parsed).Segments[1]).Name);
        Assert.IsType<IndexSegment>(Path(parsed).Segments[2]);
        Assert.Equal(1, ((IndexSegment)Path(parsed).Segments[2]).Index);
        Assert.IsType<MemberSegment>(Path(parsed).Segments[3]);
        Assert.Equal("name", ((MemberSegment)Path(parsed).Segments[3]).Name);
    }

    [Theory]
    [InlineData("weapons[0].name", "weapons", 0, "name")]
    [InlineData("player.gold_balance", "player", "gold_balance")]
    public void Parse_QuestStylePaths_MatchSpecExamples(string content, string root, params object[] tail)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        Assert.Equal(root, Path(parsed).Root);
        var i = 0;
        foreach (var item in tail)
        {
            if (item is int index)
            {
                var seg = Assert.IsType<IndexSegment>(Path(parsed).Segments[i++]);
                Assert.Equal(index, seg.Index);
            }
            else if (item is string member)
            {
                var seg = Assert.IsType<MemberSegment>(Path(parsed).Segments[i++]);
                Assert.Equal(member, seg.Name);
            }
            else
                throw new InvalidOperationException();
        }
    }

    [Theory]
    [InlineData("value:F0", "F0")]
    [InlineData("value:N2", "N2")]
    [InlineData("price:C", "C")]
    [InlineData("amount:N0", "N0")]
    [InlineData("dt:yyyy-MM-dd", "yyyy-MM-dd")]
    [InlineData("t:hh\\:mm", "hh\\:mm")]
    [InlineData("a.b:F2", "F2")]
    [InlineData("items[0].name", null)]
    public void Parse_FormatSpec_ExtractedAfterFirstColon(string content, string? expectedFormat)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        Assert.Equal(expectedFormat, parsed.Format);
    }

    [Theory]
    [InlineData("  name  ", "name")]
    [InlineData("  x  :  F0  ", "x", "F0")]
    public void Parse_Whitespace_TrimmedOnOuterExpressionAndFormat(string content, string root, string? format = null)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        Assert.Equal(root, Path(parsed).Root);
        Assert.Equal(format, parsed.Format);
    }

    [Theory]
    [InlineData(" root . member ")]
    [InlineData(" arr [ 0 ] ")]
    public void Parse_WhitespaceInsideExpression_Throws(string content)
    {
        Assert.Throws<TemplateParseException>(() => ExpressionParser.Parse(content, Pos));
    }

    [Theory]
    [InlineData("x:")]
    [InlineData("x:   ")]
    [InlineData("only:")]
    public void Parse_EmptyFormatAfterColon_BecomesNull(string content)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        Assert.Null(parsed.Format);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(":N0")]
    [InlineData(" :F0")]
    public void Parse_EmptyExpression_Throws(string content)
    {
        var ex = Assert.Throws<TemplateParseException>(() => ExpressionParser.Parse(content, Pos));
        Assert.Equal("Empty placeholder expression.", ex.Message);
        Assert.Equal(Pos, ex.Position);
    }

    [Theory]
    [InlineData(".member")]
    [InlineData("1bad")]
    [InlineData("9root")]
    [InlineData("name-")]
    [InlineData("name$")]
    [InlineData("na me")]
    [InlineData("player..health")]
    public void Parse_InvalidRootOrExpression_Throws(string content)
    {
        Assert.Throws<TemplateParseException>(() => ExpressionParser.Parse(content, Pos));
    }

    [Theory]
    [InlineData("arr[]")]
    [InlineData("arr[")]
    [InlineData("arr[abc]")]
    [InlineData("arr[-1]")]
    public void Parse_InvalidIndexSyntax_Throws(string content)
    {
        Assert.Throws<TemplateParseException>(() => ExpressionParser.Parse(content, Pos));
    }

    [Fact]
    public void Parse_IndexMissingClosingBracket_ThrowsExpectedBracket()
    {
        var ex = Assert.Throws<TemplateParseException>(() => ExpressionParser.Parse("arr[0", Pos));
        Assert.Equal("Expected ']' after index.", ex.Message);
    }

    [Theory]
    [InlineData("arr[01]", 1)]
    public void Parse_LeadingZeroIndex_ParsesAsInteger(string content, int expected)
    {
        var parsed = ExpressionParser.Parse(content, Pos);
        var seg = Assert.IsType<IndexSegment>(Path(parsed).Segments[0]);
        Assert.Equal(expected, seg.Index);
    }

    [Fact]
    public void Parse_TrailingGarbage_ThrowsUnexpectedCharacter()
    {
        var ex = Assert.Throws<TemplateParseException>(() => ExpressionParser.Parse("name!", Pos));
        Assert.Contains("Unexpected character", ex.Message);
    }

    [Fact]
    public void Parse_FormatWithMultipleColons_PreservesColonsInFormat()
    {
        var parsed = ExpressionParser.Parse("t:hh:mm:ss", Pos);
        Assert.Equal("t", Path(parsed).Root);
        Assert.Equal("hh:mm:ss", parsed.Format);
    }

    [Theory]
    [InlineData("日本語")]
    [InlineData("naïve")]
    public void Parse_NonAsciiIdentifier_Throws(string expression)
    {
        Assert.Throws<TemplateParseException>(() => ExpressionParser.Parse(expression, Pos));
    }

    [Fact]
    public void Parse_IndexAtMaxIntBoundary_Parses()
    {
        var indexText = int.MaxValue.ToString();
        var parsed = ExpressionParser.Parse($"arr[{indexText}]", Pos);
        var seg = Assert.IsType<IndexSegment>(Path(parsed).Segments[0]);
        Assert.Equal(int.MaxValue, seg.Index);
    }
}
