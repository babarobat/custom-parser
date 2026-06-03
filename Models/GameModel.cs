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
                    Description = "Kill slimes with {kill_count} using {weapons[0]} or {weapons[1]}.",
                    KillCount = 12,
                    Weapons = ["Rusty Sword", "Fireball Scroll"],
                },
                new CollectCurrencyQuest
                {
                    Id = "quest_treasury",
                    Title = "Fill the Treasury",
                    Description = "Collect {currencies[0].amount} {currencies[0].name} and {currencies[1].amount} {currencies[1].name}.Current gold: {player.gold_balance}.",
                    Currencies =
                    [
                        new CurrencyAmount("gold", "Gold", 500m),
                        new CurrencyAmount("gems", "Gems", 15m),
                    ],
                },
            ],
        };
}
