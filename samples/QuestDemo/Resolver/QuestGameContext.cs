using CustomParser.Resolver;
using QuestDemo.Models;

namespace QuestDemo.Resolver;

/// <summary>
/// Convenience context combining quest-scoped and game-scoped value providers (SPEC §3.6).
/// </summary>
public sealed class QuestGameContext : IValueProvider
{
    private readonly CompositeValueProvider _inner;

    public QuestGameContext(GameModel game, Quest quest) =>
        _inner = new CompositeValueProvider(new QuestValueProvider(quest), new ModelValueProvider(game));

    public bool TryGetValue(string key, out object? value) => _inner.TryGetValue(key, out value);

    public bool TryGetIndex(object? target, int index, out object? value) =>
        _inner.TryGetIndex(target, index, out value);

    public bool TryGetMember(object? target, string member, out object? value) =>
        _inner.TryGetMember(target, member, out value);
}
