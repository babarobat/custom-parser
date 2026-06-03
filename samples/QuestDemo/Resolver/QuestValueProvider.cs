using CustomParser.Resolver;
using QuestDemo.Models;

namespace QuestDemo.Resolver;

public sealed class QuestValueProvider : IValueProvider
{
    private readonly Quest _quest;

    public QuestValueProvider(Quest quest) => _quest = quest;

    public bool TryGetValue(string key, out object? value)
    {
        switch (key)
        {
            case "kill_count" when _quest is KillEnemyQuest kill:
                value = kill.KillCount;
                return true;
            case "weapons" when _quest is KillEnemyQuest killQuest:
                value = killQuest.Weapons;
                return true;
            case "currencies" when _quest is CollectCurrencyQuest collect:
                value = collect.Currencies;
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
        if (target is CurrencyAmount currency)
        {
            switch (member)
            {
                case "amount":
                    value = currency.Amount;
                    return true;
                case "name":
                    value = currency.Name;
                    return true;
                case "currency_id":
                    value = currency.CurrencyId;
                    return true;
            }
        }

        value = null;
        return false;
    }
}
