using custom_parser;
using custom_parser.Models;
using custom_parser.Resolver;

var game = GameModel.CreateSample();
var engine = new TemplateEngine(enableTemplateCache: true);

foreach (var quest in game.Quests)
{
    var context = new QuestGameContext(game, quest);
    var formatted = engine.Format(quest.Description, context);
    Console.WriteLine($"{quest.Title}: {formatted}");
}
