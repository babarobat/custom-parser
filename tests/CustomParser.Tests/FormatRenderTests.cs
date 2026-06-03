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

    [Fact]
    public void Format_DictionaryProvider_NestedDictionaryAndList()
    {
        var context = new DictionaryValueProvider(new Dictionary<string, object?>
        {
            ["weapons"] = new List<string> { "Sword", "Bow" },
            ["currencies"] = new List<Dictionary<string, object?>>
            {
                new() { ["amount"] = 500m, ["name"] = "Gold" },
            },
            ["player"] = new Dictionary<string, object?> { ["gold_balance"] = 42m },
        });

        var result = Engine.Format(
            "{weapons[0]} / {currencies[0].amount} {currencies[0].name} / {player.gold_balance}",
            context,
            CultureInfo.InvariantCulture);

        Assert.Equal("Sword / 500 Gold / 42", result);
    }

    [Fact]
    public void Format_DictionaryProvider_PocoMemberPath_NotResolved()
    {
        var engine = new TemplateEngine(ErrorPolicy.KeepPlaceholder);
        var context = new DictionaryValueProvider(new Dictionary<string, object?>
        {
            ["player"] = new PlayerStub { GoldBalance = 99m },
        });

        var result = engine.Format("{player.gold_balance}", context, CultureInfo.InvariantCulture);

        Assert.Equal("{player.gold_balance}", result);
    }

    private sealed class PlayerStub
    {
        public decimal GoldBalance { get; init; }
    }
}
