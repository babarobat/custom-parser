namespace QuestDemo.Models;

public abstract class Quest
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Localization description template (unresolved until a future template engine renders it).
    /// </summary>
    /// <remarks>
    /// <para>Placeholder paths use snake_case logical names for readability in loc files
    /// (e.g. <c>kill_count</c>, <c>weapons</c>, <c>currencies</c>, <c>player.gold_balance</c>).
    /// A future resolver may map these to C# properties (<c>KillCount</c>, <c>Weapons</c>, etc.)
    /// or bind case-insensitively / via camelCase — model property names are not renamed to match.</para>
    /// <para>Scopes: quest fields resolve with the quest as root — <c>{kill_count}</c>, <c>{weapons[0]}</c>.
    /// Cross-cutting values use a game-root prefix — <c>{player.gold_balance}</c>, not <c>{model.player...}</c>.</para>
    /// </remarks>
    public string Description { get; init; } = string.Empty;
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
