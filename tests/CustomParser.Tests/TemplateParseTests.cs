using CustomParser;
using CustomParser.Parser;
using CustomParser.Tests.Helpers;

namespace CustomParser.Tests;

public class TemplateParseTests
{
    private static readonly TemplateEngine Engine = new();

    [Fact]
    public void Parse_EmptyTemplate_ProducesNoSegments()
    {
        var compiled = Engine.Parse("");
        Assert.Empty(compiled.Segments);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("plain only")]
    [InlineData("日本語")]
    public void Parse_PlainTextOnly_SingleLiteralSegment(string template)
    {
        var compiled = Engine.Parse(template);
        Assert.Single(compiled.Segments);
        Assert.Equal(template, ParseTestHelpers.SingleLiteral(compiled));
    }

    [Fact]
    public void Parse_LongPlainText_SingleSegment()
    {
        var template = new string('z', 5000);
        var compiled = Engine.Parse(template);
        Assert.Equal(template, ParseTestHelpers.SingleLiteral(compiled));
    }

    [Theory]
    [InlineData("{name}", "name", null, "{name}")]
    [InlineData("{kill_count}", "kill_count", null, "{kill_count}")]
    [InlineData("{player_health:F0}", "player_health", "F0", "{player_health:F0}")]
    [InlineData("{price:N2}", "price", "N2", "{price:N2}")]
    [InlineData("{amount:C}", "amount", "C", "{amount:C}")]
    [InlineData("{x:}", "x", null, "{x:}")]
    public void Parse_SinglePlaceholder_SegmentMetadata(
        string template,
        string root,
        string? format,
        string source)
    {
        var ph = ParseTestHelpers.SinglePlaceholder(Engine.Parse(template));
        Assert.Equal(root, ph.Root);
        Assert.Equal(format, ph.Format);
        Assert.Equal(source, ph.SourceText);
        Assert.Empty(ph.Segments);
    }

    [Fact]
    public void Parse_MultiplePlaceholders_CorrectSegmentCount()
    {
        var compiled = Engine.Parse("HP: {hp:F0}/{max:F0}");
        var segments = ParseTestHelpers.DescribeSegments(compiled);
        Assert.Equal(4, segments.Count);
        Assert.Equal("HP: ", segments[0]);
        var hp = Assert.IsType<ParseTestHelpers.PlaceholderInfo>(segments[1]);
        Assert.Equal("hp", hp.Root);
        Assert.Equal("F0", hp.Format);
        Assert.Equal("/", segments[2]);
        var max = Assert.IsType<ParseTestHelpers.PlaceholderInfo>(segments[3]);
        Assert.Equal("max", max.Root);
        Assert.Equal("F0", max.Format);
    }

    [Fact]
    public void Parse_MultiplePlaceholders_FixLiteralsAndPlaceholders()
    {
        var compiled = Engine.Parse("A{a}B{b}C");
        var segments = ParseTestHelpers.DescribeSegments(compiled);
        Assert.Equal(5, segments.Count);
        Assert.Equal("A", segments[0]);
        Assert.Equal("a", Assert.IsType<ParseTestHelpers.PlaceholderInfo>(segments[1]).Root);
        Assert.Equal("B", segments[2]);
        Assert.Equal("b", Assert.IsType<ParseTestHelpers.PlaceholderInfo>(segments[3]).Root);
        Assert.Equal("C", segments[4]);
    }

    [Fact]
    public void Parse_AdjacentPlaceholders_NoLiteralSegmentsBetween()
    {
        var compiled = Engine.Parse("{x}{y}");
        var segments = ParseTestHelpers.DescribeSegments(compiled);
        Assert.Equal(2, segments.Count);
        Assert.Equal("x", Assert.IsType<ParseTestHelpers.PlaceholderInfo>(segments[0]).Root);
        Assert.Equal("y", Assert.IsType<ParseTestHelpers.PlaceholderInfo>(segments[1]).Root);
    }

    [Fact]
    public void Parse_EscapedBracesWithPlaceholder_LiteralThenPlaceholder()
    {
        var compiled = Engine.Parse("{{name}} = {name}");
        var segments = ParseTestHelpers.DescribeSegments(compiled);
        Assert.Equal(2, segments.Count);
        Assert.Equal("{name} = ", segments[0]);
        Assert.Equal("name", Assert.IsType<ParseTestHelpers.PlaceholderInfo>(segments[1]).Root);
    }

    [Fact]
    public void Parse_EscapedBracesOnly_SingleLiteral()
    {
        var compiled = Engine.Parse("use {{ and }}");
        Assert.Equal("use { and }", ParseTestHelpers.SingleLiteral(compiled));
    }

    [Fact]
    public void Parse_NestedPath_MemberAndIndexSegments()
    {
        var ph = ParseTestHelpers.SinglePlaceholder(Engine.Parse("{weapons[0].name}"));
        Assert.Equal("weapons", ph.Root);
        Assert.Equal(2, ph.Segments.Count);
        Assert.IsType<IndexSegment>(ph.Segments[0]);
        Assert.IsType<MemberSegment>(ph.Segments[1]);
        Assert.Equal("name", ((MemberSegment)ph.Segments[1]).Name);
    }

    [Fact]
    public void Parse_DeepMixedChain_AllSegments()
    {
        var ph = ParseTestHelpers.SinglePlaceholder(Engine.Parse("{items[0].stats[1].name}"));
        Assert.Equal("items", ph.Root);
        Assert.Equal(4, ph.Segments.Count);
        Assert.Equal(0, ((IndexSegment)ph.Segments[0]).Index);
        Assert.Equal("stats", ((MemberSegment)ph.Segments[1]).Name);
        Assert.Equal(1, ((IndexSegment)ph.Segments[2]).Index);
        Assert.Equal("name", ((MemberSegment)ph.Segments[3]).Name);
    }

    [Theory]
    [InlineData("{player.gold_balance}", "player", "gold_balance")]
    [InlineData("{currencies[0].name}", "currencies", 0, "name")]
    public void Parse_QuestSnakeCasePaths(string template, string root, params object[] tail)
    {
        var ph = ParseTestHelpers.SinglePlaceholder(Engine.Parse(template));
        Assert.Equal(root, ph.Root);
        var i = 0;
        foreach (var item in tail)
        {
            if (item is string member)
            {
                Assert.Equal(member, ((MemberSegment)ph.Segments[i++]).Name);
            }
            else if (item is int index)
            {
                Assert.Equal(index, ((IndexSegment)ph.Segments[i++]).Index);
            }
        }
    }

    [Fact]
    public void Parse_WhitespaceInsidePlaceholder_TrimmedInAst()
    {
        var ph = ParseTestHelpers.SinglePlaceholder(Engine.Parse("{  player_health  :  F0  }"));
        Assert.Equal("player_health", ph.Root);
        Assert.Equal("F0", ph.Format);
        Assert.Equal("{  player_health  :  F0  }", ph.SourceText);
    }

    [Fact]
    public void Parse_ManySegments_AlternatingPattern()
    {
        var template = string.Concat(Enumerable.Range(0, 20).Select(n => $"#{n}{{i{n}}}"));
        var compiled = Engine.Parse(template);
        Assert.Equal(40, compiled.Segments.Count);
        Assert.Equal(20, compiled.Segments.OfType<PlaceholderSegment>().Count());
    }

    [Theory]
    [InlineData("value}", "Unescaped '}' in template.")]
    [InlineData("text {open", "Unclosed placeholder.")]
    [InlineData("{", "Unclosed placeholder.")]
    [InlineData("{a{b}", "Unescaped '{' inside placeholder.")]
    [InlineData("{bad..path}", "Expected identifier.")]
    [InlineData("{1x}", "Expected identifier.")]
    [InlineData("{}", "Empty placeholder expression.")]
    [InlineData("{:N0}", "Empty placeholder expression.")]
    [InlineData("{arr[}", "Expected non-negative integer index.")]
    [InlineData("{arr[x]}", "Expected non-negative integer index.")]
    public void Parse_InvalidTemplates_ThrowTemplateParseException(string template, string messagePart)
    {
        var ex = Assert.Throws<TemplateParseException>(() => Engine.Parse(template));
        Assert.Contains(messagePart, ex.Message);
    }

    [Fact]
    public void Parse_CompiledTemplate_IsImmutableSnapshot()
    {
        var compiled = Engine.Parse("{a}");
        Assert.Single(compiled.Segments);
        var ph = compiled.Segments[0] as PlaceholderSegment;
        Assert.NotNull(ph);
        var path = Assert.IsType<AccessPathNode>(ph.Expression);
        Assert.Equal("a", path.Root);
    }

    [Fact]
    public void Parse_LiteralPlaceholderLiteral_ThreeSegments()
    {
        var compiled = Engine.Parse("before {x} after");
        Assert.Equal(3, compiled.Segments.Count);
        Assert.IsType<LiteralSegment>(compiled.Segments[0]);
        Assert.IsType<PlaceholderSegment>(compiled.Segments[1]);
        Assert.IsType<LiteralSegment>(compiled.Segments[2]);
    }

    [Theory]
    [InlineData("{leading} text", 2)]
    [InlineData("text {trailing}", 2)]
    [InlineData("{only}", 1)]
    public void Parse_PartialLiterals_SegmentCount(string template, int segmentCount)
    {
        Assert.Equal(segmentCount, Engine.Parse(template).Segments.Count);
    }
}
