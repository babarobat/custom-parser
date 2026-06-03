using custom_parser.Models;

namespace custom_parser.Resolver;

/// <summary>
/// Render scope for quest descriptions (game + quest). Stub: not used until resolver is implemented.
/// </summary>
public sealed class QuestGameContext : IValueProvider
{
    public QuestGameContext(GameModel game, Quest quest)
    {
        _ = game;
        _ = quest;
    }

    public object? GetValue(string key) => null;

    public object? GetIndex(object? target, int index) => null;

    public object? GetMember(object? target, string member) => null;
}
