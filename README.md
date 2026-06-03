# CustomParser

A .NET 8 library for substituting values into string templates with **named** placeholders in curly braces — similar to `string.Format`, but with paths like `{player.health}` and `{items[0].name}`.

## Install

From NuGet:

```bash
dotnet add package CustomParser
```

Or reference the library project in this repository:

```bash
dotnet add reference src/CustomParser/CustomParser.csproj
```

## Quick start

```csharp
using System.Globalization;
using CustomParser;
using CustomParser.Resolver;

var engine = new TemplateEngine();
var context = new DictionaryValueProvider(new Dictionary<string, object?>
{
    ["player_health"] = 85.7,
    ["player_health_max"] = 100,
});

var text = engine.Format(
    "HP: {player_health:F0}/{player_health_max:F0}",
    context,
    CultureInfo.InvariantCulture);
// "HP: 86/100"
```

For a hot path where the same template is rendered many times, parse once and render repeatedly:

```csharp
var compiled = engine.Parse("HP: {player_health:F0}/{player_health_max:F0}");
var text = engine.Render(compiled, context, CultureInfo.InvariantCulture);
```

See [docs/SPEC.md](docs/SPEC.md) for syntax, architecture, and error policies.

## Quest sample

The `samples/QuestDemo` project shows game models and custom providers (`QuestValueProvider`, `QuestGameContext`) that resolve quest description templates:

```bash
dotnet run --project samples/QuestDemo/QuestDemo.csproj
```

## Implementing `IValueProvider`

The resolver calls your provider while walking each placeholder path:

| Method | When it is used |
|--------|-----------------|
| `TryGetValue` | Root identifier in a path (e.g. `player` in `{player.health}`) |
| `TryGetMember` | After a `.member` segment |
| `TryGetIndex` | After an `[index]` segment |

```csharp
using CustomParser.Resolver;

public sealed class MyValueProvider : IValueProvider
{
    public bool TryGetValue(string key, out object? value)
    {
        // Map root keys to objects (dictionaries, models, etc.)
        return /* lookup */ false;
    }

    public bool TryGetIndex(object? target, int index, out object? value) =>
        MemberAccessor.TryGetIndex(target, index, out value);

    public bool TryGetMember(object? target, string member, out object? value) =>
        MemberAccessor.TryGetMember(target, member, out value);
}
```

`DictionaryValueProvider` is enough when all roots live in a dictionary. For domain-specific roots (quests, player state), implement `TryGetValue` and delegate `TryGetIndex` / `TryGetMember` to `MemberAccessor` for lists, arrays, and reflection-friendly objects — as in `samples/QuestDemo/Resolver/QuestValueProvider.cs`. Combine multiple providers with `CompositeValueProvider` or a façade like `QuestGameContext`.

Configure render-time behavior with `ErrorPolicy` on `TemplateEngine` (`Throw`, `Empty`, `KeepPlaceholder`).

## Documentation

- [docs/SPEC.md](docs/SPEC.md) — full syntax and architecture specification

## Repository layout

```
custom-parser/
├── CustomParser.sln
├── docs/SPEC.md
├── src/CustomParser/          # NuGet package (namespace CustomParser)
├── samples/QuestDemo/         # quest description demo
└── tests/CustomParser.Tests/
```

## Build and test

```bash
dotnet build CustomParser.sln
dotnet test CustomParser.sln
```

## License

MIT — see [LICENSE](LICENSE).
