# FAQ

Short answers phrased the way people search (Google, GitHub, Unity forums, AI assistants).

## Is this a replacement for `string.Format`?

For **localized or data-driven** strings that change often, yes — use **named** placeholders (`{kill_count}`, `{player.gold_balance}`) instead of `{0}`, `{1}`. You pass one `IValueProvider` context per render instead of a growing argument list at every call site.

For one-off debug lines with fixed arguments, `string.Format` is still fine.

## How is this different from SmartFormat.NET or Scriban?

CustomParser is a **small, embeddable** engine focused on:

- Named paths with `.member` and `[index]` chains
- Explicit `IValueProvider` (you control roots and quest/game scope)
- Parse-once / render-many (`CompiledTemplate`)
- Unity UPM package (precompiled DLL) plus .NET 8 / netstandard2.0

It does **not** aim to be a full scripting language (no conditionals/loops in MVP).

## Unity localization: named placeholders in quest/UI text

Install via Git UPM (see root [README](../README.md#unity-upm-via-git)). Build quest or UI strings in JSON/ScriptableObjects with `{quest_id}`, `{currencies[0].amount}`, etc., then:

```csharp
var engine = new TemplateEngine();
var text = engine.Format(descriptionFromData, new QuestGameContext(game, quest));
```

See [samples/QuestDemo](../samples/QuestDemo) for composite providers.

## Why do my placeholders stay as `{player.gold_balance}`?

Usually `TryGetValue` returns the root (`player`) but `TryGetMember` is missing or wrong on your provider type. Map **only the root key** in `TryGetValue`; let the engine walk `.` and `[]`. See README section *IValueProvider: Value, Member, Index*.

## Can designers change template names without recompiling C#?

Yes, as long as **root keys** still exist in your providers. Adding a new root (e.g. `{guild_name}`) requires a provider update; changing `{kill_count}` to `{slimes_killed}` in the locale file does not, if the provider still exposes that root.

## Where is the full syntax documented?

[SPEC.md](SPEC.md) — escaping `{{` `}}`, format specifiers (`{value:F2}`), error policies, caching.

## Repository and license

MIT — [github.com/babarobat/custom-parser](https://github.com/babarobat/custom-parser). Machine-readable index for agents: [llms.txt](../llms.txt).
