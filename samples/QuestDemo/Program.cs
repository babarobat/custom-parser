using CustomParser;
using QuestDemo.Models;
using QuestDemo.Resolver;

var game = GameModel.CreateSample();
var engine = new TemplateEngine(enableTemplateCache: true);

foreach (var quest in game.Quests)
{
    var context = new QuestGameContext(game, quest);
    var formatted = engine.Format(quest.Description, context);
    Console.WriteLine($"{quest.Title}: {formatted}");
}
