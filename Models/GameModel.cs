namespace custom_parser.Models;

public sealed class GameModel
{
    public PlayerProfile Player { get; init; } = new();

    public IList<Quest> Quests { get; init; } = new List<Quest>();

    public static GameModel CreateSample() =>
        new()
        {
            Player = new PlayerProfile
            {
                Health = 85,
                GoldBalance = 1_250.50m,
            },
            Quests =
            [
                new KillEnemyQuest
                {
                    Id = "quest_slime_hunt",
                    Title = "Slime Extermination",
                    KillCount = 12,
                    Weapons = ["Rusty Sword", "Fireball Scroll"],
                },
                new CollectCurrencyQuest
                {
                    Id = "quest_treasury",
                    Title = "Fill the Treasury",
                    Currencies =
                    [
                        new CurrencyAmount("gold", "Gold", 500m),
                        new CurrencyAmount("gems", "Gems", 15m),
                    ],
                },
            ],
        };
}
