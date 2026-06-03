# CustomParser

A .NET 8 library that resolves **named** placeholders in localization and UI strings from game state via `IValueProvider` — without tying every template change to call-site argument lists.

## The problem

Localized strings need dynamic values: counts, names, balances. Positional `string.Format` works until templates evolve.

**Locale file (positional):**

```
Kill slimes with {0} using {1} or {2}.
```

**Call site — values passed by index:**

```csharp
string.Format(quest.Description, killCount, weapons[0], weapons[1]);
```

Add a third placeholder and you update the string **and** every caller. Reorder slots for translation and the indices drift. Designers cannot rename or extend placeholders without a C# pass; the template and code stay coupled by position, not meaning.

## The solution

CustomParser uses **named** paths in `{…}` — for example `{kill_count}`, `{player.gold_balance}`, `{currencies[0].amount}`. At render time the engine walks each path and asks an `IValueProvider` for values.

**Same intent (named, from [QuestDemo](samples/QuestDemo)):**

```
Kill slimes with {kill_count} using {weapons[0]} or {weapons[1]}.
Collect {currencies[0].amount} {currencies[0].name}. Current gold: {player.gold_balance}.
```

**Call site — one pattern, no argument list:**

```csharp
var engine = new TemplateEngine();
var context = new QuestGameContext(game, quest);
var text = engine.Format(quest.Description, context);
```

Map root keys once in providers; after that, templates can change in data or loc files without editing every `Format` call. Extend a provider only when a **new** root key appears.

**Quest-scoped roots** (`QuestValueProvider`):

```csharp
public bool TryGetValue(string key, out object? value)
{
    switch (key)
    {
        case "kill_count" when _quest is KillEnemyQuest kill:
            value = kill.KillCount; return true;
        case "weapons" when _quest is KillEnemyQuest k:
            value = k.Weapons; return true;
        case "currencies" when _quest is CollectCurrencyQuest c:
            value = c.Currencies; return true;
        default:
            value = null; return false;
    }
}
```

**Game-scoped roots** (`ModelValueProvider`):

```csharp
case "player":
    value = _game.Player; return true;
```

**Composite context** (`QuestGameContext`):

```csharp
_inner = new CompositeValueProvider(
    new QuestValueProvider(quest),
    new ModelValueProvider(game));
```

**Parse once, render many** (shared template shape):

```csharp
var compiled = engine.Parse(quest.Description);
foreach (var q in game.Quests)
    Console.WriteLine(engine.Render(compiled, new QuestGameContext(game, q)));
```

For flat key–value maps only, `DictionaryValueProvider` covers simple `{key}` roots without a custom provider.

## See also

- [docs/SPEC.md](docs/SPEC.md) — syntax, path rules, caching, error policies
- [samples/QuestDemo](samples/QuestDemo) — full quest-description flow with composite providers

## Install

### Unity (UPM via Git)

Add to your project `Packages/manifest.json` **dependencies**:

```json
"com.babarobat.custom-parser": "https://github.com/babarobat/custom-parser.git?path=Packages/com.babarobat.custom-parser#v1.0.0"
```

This installs a **DLL-only** package (`Packages/com.babarobat.custom-parser/Runtime/CustomParser.dll`) — not editable source in the Unity tree. To change behavior, fork the [repository](https://github.com/babarobat/custom-parser) or embed the `src/CustomParser` sources in your project. **JetBrains Rider** can still show decompiled C# when you navigate into the assembly for reading.

Requires Unity **2021.2** or newer (see package `package.json`).

### Optional: monorepo development

When developing this library or running samples/tests **in this repository**, reference the project directly — **not** for external Unity consumers:

```xml
<ProjectReference Include="src/CustomParser/CustomParser.csproj" />
```

Adjust the relative path from your `.csproj` (for example `samples/QuestDemo/QuestDemo.csproj` uses `..\..\src\CustomParser\CustomParser.csproj`).

## Build and test

```bash
dotnet build CustomParser.sln
dotnet test CustomParser.sln
dotnet run --project samples/QuestDemo/QuestDemo.csproj
```

## License

MIT — see [LICENSE](LICENSE).
