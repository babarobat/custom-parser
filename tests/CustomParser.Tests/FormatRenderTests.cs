using System.Globalization;
using CustomParser.Resolver;

namespace CustomParser.Tests;

public class FormatRenderTests
{
    private static readonly TemplateEngine Engine = new();

    [Fact]
    public void Format_DictionaryProvider_SubstitutesValues()
    {
        var context = new DictionaryValueProvider(new Dictionary<string, object?>
        {
            ["player_health"] = 85.7,
            ["player_health_max"] = 100,
        });

        var result = Engine.Format(
            "HP: {player_health:F0}/{player_health_max:F0}",
            context,
            CultureInfo.InvariantCulture);

        Assert.Equal("HP: 86/100", result);
    }

    [Fact]
    public void Render_CompiledTemplate_MatchesFormat()
    {
        var compiled = Engine.Parse("{{name}} = {name}");
        var context = new DictionaryValueProvider(new Dictionary<string, object?> { ["name"] = "Alice" });

        var result = Engine.Render(compiled, context, CultureInfo.InvariantCulture);

        Assert.Equal("{name} = Alice", result);
    }

    [Fact]
    public void Format_MissingKey_KeepPlaceholderPolicy()
    {
        var engine = new TemplateEngine(ErrorPolicy.KeepPlaceholder);
        var context = new DictionaryValueProvider(new Dictionary<string, object?>());

        var result = engine.Format("Score: {missing_score}", context);

        Assert.Equal("Score: {missing_score}", result);
    }
}
