namespace custom_parser.Models;

public abstract class Quest
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
}

public sealed class KillEnemyQuest : Quest
{
    public int KillCount { get; init; }

    public IList<string> Weapons { get; init; } = new List<string>();
}

public sealed class CollectCurrencyQuest : Quest
{
    public IList<CurrencyAmount> Currencies { get; init; } = new List<CurrencyAmount>();
}
