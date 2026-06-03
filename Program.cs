using custom_parser.Models;

var game = GameModel.CreateSample();
Console.WriteLine($"Player health: {game.Player.Health}, gold: {game.Player.GoldBalance}");
Console.WriteLine($"Quests: {game.Quests.Count}");
foreach (var quest in game.Quests)
    Console.WriteLine(quest.Description);
