using CustomParser.Resolver;
using QuestDemo.Models;

namespace QuestDemo.Resolver;

public sealed class ModelValueProvider : IValueProvider
{
    private readonly GameModel _game;

    public ModelValueProvider(GameModel game) => _game = game;

    public bool TryGetValue(string key, out object? value)
    {
        switch (key)
        {
            case "player":
                value = _game.Player;
                return true;
            default:
                value = null;
                return false;
        }
    }

    public bool TryGetIndex(object? target, int index, out object? value) =>
        MemberAccessor.TryGetIndex(target, index, out value);

    public bool TryGetMember(object? target, string member, out object? value)
    {
        if (target is PlayerProfile player)
        {
            switch (member)
            {
                case "gold_balance":
                    value = player.GoldBalance;
                    return true;
                case "health":
                    value = player.Health;
                    return true;
            }
        }

        value = null;
        return false;
    }
}
