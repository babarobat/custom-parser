# Specification: Custom String Template Parser

> Authoritative document for architectural decisions. All implementations must follow this specification.

## 1. Purpose

A library for substituting values into string templates — similar to `string.Format`, but with **named** placeholders in curly braces.

```csharp
var text = engine.Format(
    "HP: {player_health:F0}, weapon: {weapons[0].name}",
    context,
    CultureInfo.InvariantCulture);
// "HP: 85, weapon: Sword"
```

**For library consumers**, a single `Format(template, context)` call is sufficient — no need to know about `Parse` / `Render` (see §3.6).

**For reusing the same template** (hot path): `Parse` once → `Render` many times; the result of `Parse` is a cacheable `CompiledTemplate` (§3.5).

---

## 2. Syntax (MVP)

### 2.1. Lexical structure

A template is a sequence of **literals** and **placeholders**.

```
template     ::= segment*
segment      ::= literal | placeholder
literal      ::= ( any char except unescaped '{' ) | "{{" | "}}"
placeholder  ::= '{' ws? expression ws? ( ':' format )? ws? '}'
```

### 2.2. Expression — MVP

```
expression   ::= accessPath
accessPath   ::= identifier ( ('.' identifier) | ('[' integer_literal ']') )*
```

The root `identifier` is a key in the context; zero or more access segments follow. **`.` member** and **`[index]`** segments may alternate in any order.

| Construct | Example | Description |
|---|---|---|
| Root identifier | `{player_health}` | Context key with no segments |
| Member access | `{player.health}` | One or more consecutive `.member` segments |
| Index access | `{weapons[0]}` | Collection element by integer index |
| Index then member | `{weapons[0].name}` | `.member` after `[index]` |
| Member chain | `{player.stats.health}` | Multiple consecutive `.member` segments |
| Mixed chain | `{items[0].stats[1].name}` | Arbitrary alternation of `.` and `[]` |
| Format | `{player_health:F2}` | Standard .NET format string |

**Identifier rules:** `[a-zA-Z_][a-zA-Z0-9_]*` (ASCII; dot is a segment separator only, not part of a name).

**Index rules (MVP):** non-negative integer literal only (`0`, `1`, `42`). Negative indices, string keys, and expressions inside `[]` are **not** MVP.

**Format rules:** everything after the **first** `:` up to the closing `}` is the format string. Colons inside the format are allowed (`{value:hh\\:mm}`). `:format` splitting happens **before** expression parsing.

### 2.3. Escaping

| Sequence | Result |
|---|---|
| `{{` | literal `{` |
| `}}` | literal `}` |

A lone `{` without a matching `}` or a `{` inside a placeholder — **parse error** (policy see §5).

### 2.4. Explicitly **not** in MVP

| Construct | Status |
|---|---|
| `{items[key]}` — string or computed index in `[]` | Future |
| Nested placeholders `{outer{inner}}` | Future |
| Arithmetic, function calls | Future |
| Conditionals, loops | Future |

---

## 3. Architecture (5 layers)

```
┌─────────────────────────────────────────────────────────┐
│  Engine / Orchestrator                                  │
│  Format(template, context, culture) → string  [client]  │
│  Parse(template) → CompiledTemplate  [perf path]        │
│  Render(compiled, context, culture) → string            │
└──────────┬──────────────────────────────────────────────┘
           │
     ┌─────┴─────┬─────────────┬──────────────┐
     ▼           ▼             ▼              ▼
  ┌──────┐  ┌─────────┐  ┌──────────┐  ┌───────────┐
  │ Lexer│→ │ Parser  │→ │ Resolver │→ │ Formatter │
  └──────┘  └─────────┘  └──────────┘  └───────────┘
```

### 3.1. Lexer

- **Finite state machine**, not regex.
- Knows syntax, **does not** know data.
- Output: token stream of `LiteralToken`, `PlaceholderToken` (raw string content of `{...}` without braces).

### 3.2. Expression Parser

- Parses `PlaceholderToken` content into an AST.
- Splits `:format` first, then parses the expression.
- AST nodes (MVP): `AccessPathNode` + segments `MemberSegment` / `IndexSegment`.

```
AST (MVP):

  AccessPathNode(root: string, segments: AccessSegment[])
  AccessSegment ::= MemberSegment | IndexSegment
  MemberSegment(member: string)
  IndexSegment(index: int)

  // Examples:
  // {player_health}           → AccessPathNode("player_health", [])
  // {player.stats.health}     → AccessPathNode("player", [Member, Member])
  // {weapons[0].name}         → AccessPathNode("weapons", [Index, Member])
  // {items[0].stats[1].name}  → AccessPathNode("items", [Index, Member, Index, Member])
```

### 3.3. Resolver

- Walks the AST, reading values from `IValueProvider`.
- Knows data, **does not** format strings.
- Returns `object?` (via `ResolveResult` internally).

### 3.4. Formatter

- Applies `IFormattable.ToString(format, culture)` or `ToString()`.
- Receives `CultureInfo` from `Render`.

### 3.5. Engine / Orchestrator

- Coordinates the layers.
- **`Format`** (§3.6) — primary client entry: internally `Parse` + `Render` in one call.
- **`Parse`** → `CompiledTemplate` (segment list: literal | placeholder descriptor) — **advanced / performance path**.
- **`Render`** → concatenation of literals + formatted values — **advanced / performance path**.

Optional: `TemplateEngine(..., enableTemplateCache: true)` caches `CompiledTemplate` by template string inside `Format`.

### 3.6. Client API (convenient entry for consumers)

The consumer supplies only:

1. **Template string** with `{...}` placeholders (e.g. `quest.Description` from data).
2. **`IValueProvider` context** — where placeholder values are read from.

The internal `Parse` → `Render` flow **does not** need to be visible to the client.

#### `Format` method

**Signature (instance method on `TemplateEngine`):**

```csharp
public string Format(string template, IValueProvider context, CultureInfo? culture = null);
```

| Parameter | Purpose |
|---|---|
| `template` | Source string with `{placeholder}` |
| `context` | Value provider for one render (see below) |
| `culture` | Culture for `:format` (`F0`, `N2`, dates, etc.); `null` → `CultureInfo.CurrentCulture` |

**Behavior:** equivalent to `Render(Parse(template), context, culture)`. Error policy comes from the `TemplateEngine` constructor (`ErrorPolicy`).

**Static vs instance:** **instance method** is fixed — error policy and future engine settings are bound to the instance. Typical usage: one shared `TemplateEngine` (e.g. with `ErrorPolicy.KeepPlaceholder` for UI) or a local instance with defaults.

**`CompiledTemplate` caching:** by default `Format` parses the template on every call. Pass `enableTemplateCache: true` to cache by template string, or use explicit `Parse` + `Render` for full control.

#### Context for the client

The client builds **one** `IValueProvider` per render scope (e.g. current game + active quest), not separate dictionaries per placeholder.

```csharp
// Example: quest description
var text = engine.Format(quest.Description, context);
```

**Future (not MVP):** a composite provider or type such as `QuestGameContext` that merges quest root (`kill_count`, `weapons[0]`, …) and game root (`player.gold_balance`) into one `IValueProvider` — the client still passes a single `context`.

#### When to use `Parse` / `Render`

| Scenario | API |
|---|---|
| One-off substitution (descriptions, tooltips, logs) | `Format` |
| Same template rendered many times with different context | `Parse` → store `CompiledTemplate` → `Render` in a loop |
| Cache compiled templates in a game | `Parse` on load, `Render` on UI update |

---

## 4. Interfaces (sketch)

```csharp
namespace CustomParser;

// ── Engine ──

public enum ErrorPolicy { Throw, Empty, KeepPlaceholder }

public sealed class TemplateEngine
{
    public TemplateEngine(ErrorPolicy errorPolicy = ErrorPolicy.Throw, bool enableTemplateCache = false);

    /// <summary>Primary client API: Parse + Render in one call.</summary>
    public string Format(string template, IValueProvider context, CultureInfo? culture = null);

    /// <summary>Performance path: parse once, render many times.</summary>
    public CompiledTemplate Parse(string template);
    public string Render(CompiledTemplate compiled, IValueProvider context, CultureInfo? culture = null);
}

public sealed class CompiledTemplate
{
    // Immutable; safe for caching and concurrent reads.
    internal IReadOnlyList<TemplateSegment> Segments { get; }
}

// ── Value Provider ──

namespace CustomParser.Resolver;

public interface IValueProvider
{
    bool TryGetValue(string key, out object? value);
    bool TryGetIndex(object? target, int index, out object? value);
    bool TryGetMember(object? target, string member, out object? value);
}

public sealed class DictionaryValueProvider : IValueProvider
{
    public DictionaryValueProvider(IReadOnlyDictionary<string, object?> data);
    // TryGetIndex / TryGetMember delegate to MemberAccessor (see below).
}

public sealed class CompositeValueProvider : IValueProvider
{
    public CompositeValueProvider(params IValueProvider[] providers);
    // First provider that succeeds wins for each TryGet* call.
}

/// <summary>
/// Default nested access for dictionary/list targets — no POCO reflection.
/// </summary>
public static class MemberAccessor
{
    public static bool TryGetIndex(object? target, int index, out object? value);
    // IList only (arrays implement IList).

    public static bool TryGetMember(object? target, string member, out object? value);
    // IDictionary&lt;string, object?&gt; only — no reflection on arbitrary objects.
}

// ── Lexer ──

namespace CustomParser.Lexer;

internal enum TokenKind { Literal, Placeholder }

internal readonly record struct Token(TokenKind Kind, string Value, int Position);

internal static class TemplateLexer
{
    internal static IReadOnlyList<Token> Tokenize(string template);
}

// ── Parser ──

namespace CustomParser.Parser;

internal abstract record AstNode;

internal sealed record AccessPathNode(string Root, IReadOnlyList<AccessSegment> Segments) : AstNode;

internal abstract record AccessSegment;
internal sealed record MemberSegment(string Name) : AccessSegment;
internal sealed record IndexSegment(int Index) : AccessSegment;

internal sealed record ParsedPlaceholder(AstNode Expression, string? Format);

internal static class ExpressionParser
{
    internal static ParsedPlaceholder Parse(string placeholderContent, int position);
}

// ── Resolver ──

internal static class ValueResolver
{
    internal static ResolveResult Resolve(AstNode node, IValueProvider context);
}

// ── Formatter ──

namespace CustomParser.Formatter;

internal static class ValueFormatter
{
    internal static string Format(object? value, string? format, CultureInfo culture);
}
```

**Nested access policy:** `DictionaryValueProvider` uses `MemberAccessor` for `TryGetIndex` / `TryGetMember`. `MemberAccessor` supports `IList` (including arrays) and `IDictionary<string, object?>` only — **no reflection** on POCO properties. For model objects, implement `TryGetMember` / `TryGetIndex` on custom providers (see QuestDemo sample).

---

## 5. Error policy

Configured via `ErrorPolicy` when creating `TemplateEngine`. Applied at **Render** (resolution/formatting errors) and **Parse** (syntax errors — always `Throw`).

| Policy | Behavior on resolution failure |
|---|---|
| `Throw` | `TemplateRenderException` with position and placeholder text |
| `Empty` | Empty string `""` |
| `KeepPlaceholder` | Original `{...}` text unchanged |

**Parse-time errors** (unclosed `{`, invalid expression) — always throw `TemplateParseException`, regardless of policy.

**Null values:** not an error; formatted as an empty string (fixed: **empty string**, not `"null"`).

---

## 6. Culture

`CultureInfo` is passed to `Format(..., culture)` and `Render(..., culture)`.

- `culture == null` → `CultureInfo.CurrentCulture`.
- Used only in `Formatter` (numbers, dates).
- **When it matters for clients:** placeholders with numeric/currency format (`{price:N2}`, `{amount:C}`) — thousand separators and currency symbols depend on culture; for fixed display (debug, export) pass `CultureInfo.InvariantCulture` explicitly.

---

## 7. MVP vs Future

| Feature | MVP | Future |
|---|---|---|
| `{name}` | ✅ | |
| `{obj.field}` | ✅ | |
| `{a.b.c}` chains | ✅ | |
| `{arr[0]}` | ✅ | |
| `{arr[0].field}` | ✅ | |
| `{a[0].b[1].c}` mixed `.` / `[]` | ✅ | |
| `{arr[key]}` string / expr index | | ✅ |
| `:format` | ✅ | |
| `{{` / `}}` | ✅ | |
| `Format` (client sugar: Parse + Render) | ✅ | |
| Parse once / Render many | ✅ | (advanced path) |
| `IValueProvider` | ✅ | |
| `ErrorPolicy` | ✅ | |
| `CultureInfo` | ✅ | |
| Nested placeholders | | ✅ |
| Arithmetic / functions | | ✅ |
| Custom formatters | | ✅ |
| Async Render | | ✅ |

---

## 8. Examples

### 8.1. Basic

```
Template:  "Hello, {name}!"
Context:   { "name" → "Alice" }
Result:    "Hello, Alice!"
```

### 8.2. Member, index, and mixed chains

```
Template:  "HP detail: {player.stats.health}"
Context:   { "player" → { stats → { health → 85 } } }
Result:    "HP detail: 85"

Template:  "Weapon: {weapons[0].name} (dmg: {weapons[0].damage})"
Context:   { "weapons" → [ { name: "Sword", damage: 10 }, ... ] }
Result:    "Weapon: Sword (dmg: 10)"

Template:  "Item: {items[0].stats[1].name}"
Context:   { "items" → [ { stats → [ ..., { name: "Ruby" } ] } ] }
Result:    "Item: Ruby"
```

### 8.3. Format

```
Template:  "HP: {player_health:F0}/{player_health_max:F0}"
Context:   { "player_health" → 85.7, "player_health_max" → 100 }
Result:    "HP: 86/100"
```

### 8.4. Escaping

```
Template:  "{{name}} = {name}"
Result:    "{name} = Alice"
```

### 8.5. ErrorPolicy

```
Template:  "Score: {missing_score}"
KeepPlaceholder → "Score: {missing_score}"
Empty           → "Score: "
Throw           → TemplateRenderException
```

---

## 9. Project structure

```
custom-parser/
├── LICENSE
├── README.md
├── CustomParser.sln
├── docs/
│   └── SPEC.md
├── src/CustomParser/              ← NuGet-ready library (namespace CustomParser)
│   ├── CustomParser.csproj        ← net8.0; netstandard2.0 (Unity)
│   ├── Engine/
│   ├── Lexer/
│   ├── Parser/
│   ├── Formatter/
│   ├── Resolver/                  ← IValueProvider, DictionaryValueProvider, MemberAccessor, …
│   └── Polyfills/                 ← netstandard2.0 shims (e.g. IsExternalInit)
├── samples/QuestDemo/             ← quest description demo (Exe, net8.0)
│   ├── Program.cs
│   ├── Models/
│   └── Resolver/                  ← QuestValueProvider, QuestGameContext, …
└── tests/CustomParser.Tests/
```

---

## 10. Fixed decisions (checklist)

- [x] Lexer — state machine, not regex
- [x] Client API: `Format(template, context)` — Parse + Render without exposing internals
- [x] Parse once → `CompiledTemplate`, Render many (performance path)
- [x] `IValueProvider` + `DictionaryValueProvider` by default
- [x] `MemberAccessor` — `IList` + `IDictionary<string, object?>` only; no POCO reflection
- [x] `ErrorPolicy`: Throw / Empty / KeepPlaceholder
- [x] `CultureInfo` in `Render`
- [x] Lexer knows syntax, Resolver knows data, Formatter knows formatting
- [x] MVP expression: `accessPath` — identifier + arbitrary `.member` / `[int]` chain
- [x] MVP index: integer literal ≥ 0 only
- [x] Format split before expression parsing
- [x] Null → empty string
- [x] Parse errors → always throw
- [x] Multi-target: netstandard2.0 + net8.0

---

## Appendix A. Quest description templates (sample data)

Quest `Description` strings in `Models` use **snake_case** logical placeholder paths for localization (`kill_count`, `weapons`, `currencies`, `player.gold_balance`). They need not match C# property names (`KillCount`, `GoldBalance`); a future quest-aware resolver may alias or bind case-insensitively.

**Client rendering:** one `Format` call with a context covering both quest and game:

```csharp
var description = engine.Format(quest.Description, context);
```

Here `context` is one `IValueProvider` for the "game + current quest" scope (see §3.6); typical UI does not need separate `Parse`/`Render` calls.

| Scope | Convention | Example |
|---|---|---|
| Quest root | Placeholders resolve against the quest instance | `{kill_count}`, `{weapons[0]}`, `{currencies[0].name}` |
| Game root | Cross-cutting values prefix with `player.` | `{player.gold_balance}` |
