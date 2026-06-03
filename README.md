# CustomParser

A .NET 8 library that resolves **named** placeholders in localization and UI strings from game state via `IValueProvider` — without tying every template change to call-site argument lists.

**Repository:** [github.com/babarobat/custom-parser](https://github.com/babarobat/custom-parser) · **Unity UPM:** `com.babarobat.custom-parser` · **License:** MIT

Use this when you need **named** template placeholders (`{kill_count}`, `{player.gold_balance}`, `{weapons[0].name}`) instead of positional `{0}`, `{1}` — common in **Unity game localization**, quest/UI copy, and any data-driven strings that translators or designers change without touching every `string.Format` call site.

| Looking for… | This project |
|--------------|--------------|
| `string.Format` with `{0}` index drift | Named paths + single `IValueProvider` context |
| Unity quest / HUD text from JSON or loc tables | [QuestDemo](samples/QuestDemo), UPM install below |
| Parse once, render many (hot path) | `engine.Parse` → `engine.Render` |
| Full syntax and errors | [docs/SPEC.md](docs/SPEC.md) |
| Common questions | [docs/FAQ.md](docs/FAQ.md) |
| AI / agent index | [llms.txt](llms.txt) |

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

## IValueProvider: Value, Member, Index

When you implement `IValueProvider`, the engine resolves each `{…}` path in steps — not as one string lookup.

| Method | When it runs |
|--------|----------------|
| **TryGetValue** | Root key only: first name after `{`, before the first `.` or `[` |
| **TryGetMember** | Each `.field` on the current object |
| **TryGetIndex** | Each `[n]` on the current list or array |

**Examples:**

- `{kill_count}` → `TryGetValue("kill_count")`
- `{player.gold_balance}` → `TryGetValue("player")` → `TryGetMember(…, "gold_balance")`
- `{weapons[0]}` → `TryGetValue("weapons")` → `TryGetIndex(…, 0)`
- `{currencies[0].amount}` → `TryGetValue("currencies")` → `TryGetIndex(…, 0)` → `TryGetMember(…, "amount")`
- `{player.battle_pass.achievements[2].target.amount}` → Value(`"player"`) → Member(`"battle_pass"`) → Member(`"achievements"`) → Index(`2`) → Member(`"target"`) → Member(`"amount"`)

**Common mistake:** do not handle the full path inside `TryGetValue` (e.g. `case "player.gold_balance"`). Map only root keys there; return the object and let the engine call `TryGetMember` / `TryGetIndex` for the rest.

In [QuestDemo](samples/QuestDemo), quest roots (`kill_count`, `weapons`, `currencies`) live in `QuestValueProvider`; game roots (`player`) in `ModelValueProvider`. `CompositeValueProvider` merges both into one render context.

## See also

- [docs/SPEC.md](docs/SPEC.md) — syntax, path rules, caching, error policies
- [docs/FAQ.md](docs/FAQ.md) — Unity localization, `IValueProvider` pitfalls, vs `string.Format`
- [llms.txt](llms.txt) — curated links for AI agents and tooling
- [samples/QuestDemo](samples/QuestDemo) — full quest-description flow with composite providers

## Install

### Unity (UPM via Git)

Add to your project `Packages/manifest.json` **dependencies**:

```json
"com.babarobat.custom-parser": "https://github.com/babarobat/custom-parser.git?path=Packages/com.babarobat.custom-parser#v1.0.3"
```

This installs a **DLL-only** package (`Packages/com.babarobat.custom-parser/Runtime/CustomParser.dll`) — not editable source in the Unity tree. To change behavior, fork the [repository](https://github.com/babarobat/custom-parser) or embed the `src/CustomParser` sources in your project. **JetBrains Rider** can still show decompiled C# when you navigate into the assembly for reading.

Maintainers: open this repo's UPM folder in an empty Unity project and commit Unity-generated `.meta` files (do not hand-write GUIDs). Consumers: pin **`#v1.0.3`** (or newer); **v1.0.0** lacked metas and assets were ignored.

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
