using CustomParser;
using CustomParser.Lexer;

namespace CustomParser.Tests.Lexer;

public class LexerTests
{
    [Fact]
    public void Tokenize_Empty_ReturnsNoTokens()
    {
        var tokens = TemplateLexer.Tokenize("");
        Assert.Empty(tokens);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("plain text only")]
    [InlineData("no braces here at all")]
    [InlineData("line1\nline2\ttab")]
    [InlineData("日本語リテラル")]
    [InlineData("emoji 🎮 literal")]
    [InlineData("path\\to\\file")]
    [InlineData("100% complete")]
    public void Tokenize_PlainTextOnly_ProducesSingleLiteral(string template)
    {
        var tokens = TemplateLexer.Tokenize(template);
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Literal, tokens[0].Kind);
        Assert.Equal(template, tokens[0].Value);
        Assert.Equal(0, tokens[0].Position);
    }

    [Fact]
    public void Tokenize_LongLiteral_PreservesContent()
    {
        var template = new string('x', 10_000);
        var tokens = TemplateLexer.Tokenize(template);
        Assert.Single(tokens);
        Assert.Equal(template, tokens[0].Value);
    }

    [Theory]
    [InlineData("{name}", "name")]
    [InlineData("{player_health}", "player_health")]
    [InlineData("{ kill_count }", " kill_count ")]
    [InlineData("{a:F0}", "a:F0")]
    [InlineData("{items[0].name}", "items[0].name")]
    public void Tokenize_SinglePlaceholderOnly_NoEmptyLiteralToken(string template, string expectedContent)
    {
        var tokens = TemplateLexer.Tokenize(template);
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Placeholder, tokens[0].Kind);
        Assert.Equal(expectedContent, tokens[0].Value);
    }

    [Theory]
    [InlineData("{a}{b}", 2, 2)]
    [InlineData("{x}{y}{z}", 3, 3)]
    [InlineData("pre{a}mid{b}post", 2, 5)]
    [InlineData("{first} and {second}", 2, 3)]
    public void Tokenize_MultiplePlaceholders_CountAndKind(
        string template,
        int placeholderCount,
        int expectedTokenCount)
    {
        var tokens = TemplateLexer.Tokenize(template);
        Assert.Equal(placeholderCount, tokens.Count(t => t.Kind == TokenKind.Placeholder));
        Assert.Equal(expectedTokenCount, tokens.Count);
    }

    [Fact]
    public void Tokenize_AdjacentPlaceholders_OnlyPlaceholderTokens()
    {
        var tokens = TemplateLexer.Tokenize("{a}{b}");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Placeholder, tokens[0].Kind);
        Assert.Equal("a", tokens[0].Value);
        Assert.Equal(TokenKind.Placeholder, tokens[1].Kind);
        Assert.Equal("b", tokens[1].Value);
    }

    [Theory]
    [InlineData("{{", "{")]
    [InlineData("}}", "}")]
    [InlineData("{{name}}", "{name}")]
    [InlineData("a{{b}}c", "a{b}c")]
    [InlineData("{{ }}", "{ }")]
    public void Tokenize_EscapedBraces_ProduceLiteralBraces(string template, string expectedLiteral)
    {
        var tokens = TemplateLexer.Tokenize(template);
        Assert.Single(tokens);
        Assert.Equal(expectedLiteral, tokens[0].Value);
    }

    [Fact]
    public void Tokenize_PlaceholderOnly_NoLiteralTokenEmitted()
    {
        var tokens = TemplateLexer.Tokenize("{only}");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Placeholder, tokens[0].Kind);
        Assert.Equal("only", tokens[0].Value);
    }

    [Theory]
    [InlineData("start {a} middle {b} end", "start ", "a", " middle ", "b", " end")]
    public void Tokenize_LiteralsAroundPlaceholders_SplitCorrectly(
        string template,
        string lit1,
        string ph1,
        string lit2,
        string ph2,
        string lit3)
    {
        var tokens = TemplateLexer.Tokenize(template);
        Assert.Equal(5, tokens.Count);
        Assert.Equal(lit1, tokens[0].Value);
        Assert.Equal(ph1, tokens[1].Value);
        Assert.Equal(lit2, tokens[2].Value);
        Assert.Equal(ph2, tokens[3].Value);
        Assert.Equal(lit3, tokens[4].Value);
    }

    [Fact]
    public void Tokenize_LiteralPlaceholderLiteral_ThreeTokens()
    {
        var tokens = TemplateLexer.Tokenize("before {x} after");
        Assert.Equal(3, tokens.Count);
        Assert.Equal("before ", tokens[0].Value);
        Assert.Equal("x", tokens[1].Value);
        Assert.Equal(" after", tokens[2].Value);
    }

    [Fact]
    public void Tokenize_ManyAlternatingSegments()
    {
        var template = string.Concat(Enumerable.Range(0, 50).Select(i => $"L{i}{{{i}}}"));
        var tokens = TemplateLexer.Tokenize(template);
        Assert.Equal(100, tokens.Count);
        Assert.Equal(50, tokens.Count(t => t.Kind == TokenKind.Placeholder));
        Assert.Equal(50, tokens.Count(t => t.Kind == TokenKind.Literal));
    }

    [Fact]
    public void Tokenize_LoneClosingBrace_ThrowsUnescaped()
    {
        var ex = Assert.Throws<TemplateParseException>(() => TemplateLexer.Tokenize("value}"));
        Assert.Equal("Unescaped '}' in template.", ex.Message);
        Assert.Equal(5, ex.Position);
    }

    [Fact]
    public void Tokenize_SingleOpenBraceWithoutPair_ThrowsUnclosed()
    {
        var ex = Assert.Throws<TemplateParseException>(() => TemplateLexer.Tokenize("text {open"));
        Assert.Equal("Unclosed placeholder.", ex.Message);
        Assert.Equal(5, ex.Position);
    }

    [Fact]
    public void Tokenize_UnclosedAtEnd_ThrowsWithStartPosition()
    {
        var ex = Assert.Throws<TemplateParseException>(() => TemplateLexer.Tokenize("{never"));
        Assert.Equal("Unclosed placeholder.", ex.Message);
        Assert.Equal(0, ex.Position);
    }

    [Theory]
    [InlineData("{a{b}")]
    [InlineData("{ {name} }")]
    [InlineData("{player{health}}")]
    public void Tokenize_BraceInsidePlaceholder_ThrowsUnescaped(string template)
    {
        var ex = Assert.Throws<TemplateParseException>(() => TemplateLexer.Tokenize(template));
        Assert.Equal("Unescaped '{' inside placeholder.", ex.Message);
        Assert.True(ex.Position >= 0);
    }

    [Fact]
    public void Tokenize_TrailingEscapedOpenBrace_LiteralOnly()
    {
        var tokens = TemplateLexer.Tokenize("end{{");
        Assert.Single(tokens);
        Assert.Equal("end{", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_PlaceholderPositions_RecordedAtOpenBrace()
    {
        var tokens = TemplateLexer.Tokenize("xx{a}yy{b}");
        var placeholders = tokens.Where(t => t.Kind == TokenKind.Placeholder).ToList();
        Assert.Equal(2, placeholders.Count);
        Assert.Equal(2, placeholders[0].Position);
        Assert.Equal(7, placeholders[1].Position);
    }
}
