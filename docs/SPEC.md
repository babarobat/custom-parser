# Спецификация: Custom String Template Parser

> Авторитетный документ архитектурных решений. Все реализации должны следовать этой спецификации.

## 1. Назначение

Библиотека для подстановки значений в строковые шаблоны — аналог `string.Format`, но с **именованными** плейсхолдерами в фигурных скобках.

```csharp
var template = engine.Parse("HP: {player_health:F0}, weapon: {weapons[0].name}");
var result = engine.Render(template, context, CultureInfo.InvariantCulture);
// "HP: 85, weapon: Sword"
```

**Ключевой принцип:** разбор выполняется один раз (`Parse`), рендер — многократно (`Render`). Результат `Parse` — кэшируемый `CompiledTemplate`.

---

## 2. Синтаксис (MVP)

### 2.1. Лексическая структура

Шаблон — последовательность **литералов** и **плейсхолдеров**.

```
template     ::= segment*
segment      ::= literal | placeholder
literal      ::= ( any char except unescaped '{' ) | "{{" | "}}"
placeholder  ::= '{' ws? expression ws? ( ':' format )? ws? '}'
```

### 2.2. Выражение (expression) — MVP

```
expression   ::= accessPath
accessPath   ::= identifier ( ('.' identifier) | ('[' integer_literal ']') )*
```

Корневой `identifier` — ключ в контексте; далее ноль или более сегментов доступа. Сегменты **`.` member** и **`[index]`** могут чередоваться в любом порядке.

| Конструкция | Пример | Описание |
|---|---|---|
| Корневой идентификатор | `{player_health}` | Ключ в контексте без сегментов |
| Доступ к члену | `{player.health}` | Одно или несколько `.member` подряд |
| Индексный доступ | `{weapons[0]}` | Элемент коллекции по целочисленному индексу |
| Индекс, затем член | `{weapons[0].name}` | `.member` после `[index]` |
| Цепочка членов | `{player.stats.health}` | Несколько `.member` подряд |
| Смешанная цепочка | `{items[0].stats[1].name}` | Произвольное чередование `.` и `[]` |
| Формат | `{player_health:F2}` | Стандартная .NET-строка формата |

**Правила идентификаторов:** `[a-zA-Z_][a-zA-Z0-9_]*` (ASCII; точка — только разделитель сегментов, не часть имени).

**Правила индекса (MVP):** только неотрицательное целочисленное литеральное значение (`0`, `1`, `42`). Отрицательные индексы, строковые ключи и выражения внутри `[]` — **не** MVP.

**Правила формата:** всё после **первого** `:` до закрывающей `}` — строка формата. Двоеточия внутри формата допустимы (`{value:hh\\:mm}`). Разделение `:format` выполняется **до** парсинга выражения.

### 2.3. Экранирование

| Последовательность | Результат |
|---|---|
| `{{` | литеральный `{` |
| `}}` | литеральный `}` |

Одиночная `{` без парной `}` или `{` внутри плейсхолдера — **ошибка парсинга** (политика см. §6).

### 2.4. Явно **не** входит в MVP

| Конструкция | Статус |
|---|---|
| `{items[key]}` — строковый или вычисляемый индекс в `[]` | Future |
| Вложенные плейсхолдеры `{outer{inner}}` | Future |
| Арифметика, вызовы функций | Future |
| Условия, циклы | Future |

---

## 3. Архитектура (5 слоёв)

```
┌─────────────────────────────────────────────────────────┐
│  Engine / Orchestrator                                  │
│  Parse(template) → CompiledTemplate                     │
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

- **Конечный автомат**, не regex.
- Знает синтаксис, **не** знает данные.
- Выход: поток токенов `LiteralToken`, `PlaceholderToken` (сырая строка содержимого `{...}` без скобок).

### 3.2. Expression Parser

- Парсит содержимое `PlaceholderToken` в AST.
- Сначала отделяет `:format`, затем парсит выражение.
- Узлы AST (MVP): `AccessPathNode` + сегменты `MemberSegment` / `IndexSegment`.

```
AST (MVP):

  AccessPathNode(root: string, segments: AccessSegment[])
  AccessSegment ::= MemberSegment | IndexSegment
  MemberSegment(member: string)
  IndexSegment(index: int)

  // Примеры:
  // {player_health}           → AccessPathNode("player_health", [])
  // {player.stats.health}     → AccessPathNode("player", [Member, Member])
  // {weapons[0].name}         → AccessPathNode("weapons", [Index, Member])
  // {items[0].stats[1].name}  → AccessPathNode("items", [Index, Member, Index, Member])
```

### 3.3. Resolver

- Обходит AST, читая значения из `IValueProvider`.
- Знает данные, **не** форматирует строки.
- Возвращает `object?` (или typed wrapper).

### 3.4. Formatter

- Применяет `IFormattable.ToString(format, culture)` или `ToString()`.
- Получает `CultureInfo` из `Render`.

### 3.5. Engine / Orchestrator

- Координирует слои.
- `Parse` → `CompiledTemplate` (список сегментов: литерал | resolved-placeholder-descriptor).
- `Render` → конкатенация литералов + отформатированных значений.

---

## 4. Интерфейсы (скетч)

```csharp
namespace custom_parser;

// ── Engine ──

public enum ErrorPolicy { Throw, Empty, KeepPlaceholder }

public sealed class TemplateEngine
{
    public TemplateEngine(ErrorPolicy errorPolicy = ErrorPolicy.Throw);

    public CompiledTemplate Parse(string template);
    public string Render(CompiledTemplate compiled, IValueProvider context, CultureInfo? culture = null);
}

public sealed class CompiledTemplate
{
    // Immutable; безопасен для кэширования и многопоточного чтения.
    internal IReadOnlyList<TemplateSegment> Segments { get; }
}

// ── Value Provider ──

public interface IValueProvider
{
    object? GetValue(string key);
    object? GetIndex(object? target, int index);
    object? GetMember(object? target, string member);
}

public sealed class DictionaryValueProvider : IValueProvider
{
    public DictionaryValueProvider(IReadOnlyDictionary<string, object?> data);
    // GetIndex: target is IList / array
    // GetMember: target — reflection или Dictionary<string, object?>
}

// ── Lexer ──

internal enum TokenKind { Literal, Placeholder }

internal readonly record struct Token(TokenKind Kind, string Value, int Position);

internal static class Lexer
{
    internal static IReadOnlyList<Token> Tokenize(string template);
}

// ── Parser ──

internal abstract record AstNode;

internal sealed record AccessPathNode(string Root, IReadOnlyList<AccessSegment> Segments) : AstNode;

internal abstract record AccessSegment;
internal sealed record MemberSegment(string Name) : AccessSegment;
internal sealed record IndexSegment(int Index) : AccessSegment;

internal sealed record ParsedPlaceholder(AstNode Expression, string? Format);

internal static class ExpressionParser
{
    internal static ParsedPlaceholder Parse(string placeholderContent);
}

// ── Resolver ──

internal static class ValueResolver
{
    internal static object? Resolve(AstNode node, IValueProvider context, ErrorPolicy policy);
}

// ── Formatter ──

internal static class ValueFormatter
{
    internal static string Format(object? value, string? format, CultureInfo culture);
}
```

---

## 5. Политика ошибок

Настраивается через `ErrorPolicy` при создании `TemplateEngine`. Применяется на этапе **Render** (ошибки разрешения/форматирования) и **Parse** (синтаксические ошибки — всегда `Throw`).

| Политика | Поведение при ошибке разрешения |
|---|---|
| `Throw` | `TemplateRenderException` с позицией и именем плейсхолдера |
| `Empty` | Пустая строка `""` |
| `KeepPlaceholder` | Исходный текст `{...}` без изменений |

**Parse-time ошибки** (незакрытая `{`, невалидное выражение) — всегда исключение, независимо от политики.

**Null-значения:** не ошибка; форматируются как пустая строка (или `"null"` — зафиксировано: **пустая строка**).

---

## 6. Культура

`CultureInfo` передаётся в `Render(compiled, context, culture)`.

- `culture == null` → `CultureInfo.CurrentCulture`.
- Используется только в `Formatter` (числа, даты).

---

## 7. MVP vs Future

| Возможность | MVP | Future |
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
| Parse once / Render many | ✅ | |
| `IValueProvider` | ✅ | |
| `ErrorPolicy` | ✅ | |
| `CultureInfo` | ✅ | |
| Вложенные плейсхолдеры | | ✅ |
| Арифметика / функции | | ✅ |
| Кастомные форматтеры | | ✅ |
| Async Render | | ✅ |

---

## 8. Примеры

### 8.1. Базовый

```
Шаблон:  "Hello, {name}!"
Контекст: { "name" → "Alice" }
Результат: "Hello, Alice!"
```

### 8.2. Член, индекс и смешанные цепочки

```
Шаблон:  "HP detail: {player.stats.health}"
Контекст: { "player" → { stats → { health → 85 } } }
Результат: "HP detail: 85"

Шаблон:  "Weapon: {weapons[0].name} (dmg: {weapons[0].damage})"
Контекст: { "weapons" → [ { name: "Sword", damage: 10 }, ... ] }
Результат: "Weapon: Sword (dmg: 10)"

Шаблон:  "Item: {items[0].stats[1].name}"
Контекст: { "items" → [ { stats → [ ..., { name: "Ruby" } ] } ] }
Результат: "Item: Ruby"
```

### 8.3. Формат

```
Шаблон:  "HP: {player_health:F0}/{player_health_max:F0}"
Контекст: { "player_health" → 85.7, "player_health_max" → 100 }
Результат: "HP: 86/100"
```

### 8.4. Экранирование

```
Шаблон:  "{{name}} = {name}"
Результат: "{name} = Alice"
```

### 8.5. ErrorPolicy

```
Шаблон:  "Score: {missing_score}"
KeepPlaceholder → "Score: {missing_score}"
Empty           → "Score: "
Throw           → TemplateRenderException
```

---

## 9. Структура проекта

```
custom-parser/
├── docs/
│   └── SPEC.md              ← этот документ
├── Engine/
│   ├── TemplateEngine.cs
│   ├── CompiledTemplate.cs
│   └── ErrorPolicy.cs
├── Lexer/
│   ├── Lexer.cs
│   └── Token.cs
├── Parser/
│   ├── ExpressionParser.cs
│   └── AstNodes.cs
├── Resolver/
│   ├── IValueProvider.cs
│   ├── DictionaryValueProvider.cs
│   └── ValueResolver.cs
├── Formatter/
│   └── ValueFormatter.cs
└── Program.cs               ← демо / тесты (временно)
```

---

## 10. Зафиксированные решения (чеклист)

- [x] Lexer — state machine, не regex
- [x] Parse once → `CompiledTemplate`, Render many
- [x] `IValueProvider` + `DictionaryValueProvider` по умолчанию
- [x] `ErrorPolicy`: Throw / Empty / KeepPlaceholder
- [x] `CultureInfo` в `Render`
- [x] Lexer знает синтаксис, Resolver знает данные, Formatter знает форматирование
- [x] MVP expression: `accessPath` — identifier + произвольная цепочка `.member` / `[int]`
- [x] MVP index: только integer literal ≥ 0
- [x] Format отделяется до парсинга expression
- [x] Null → пустая строка
- [x] Parse errors → always throw

---

## Appendix A. Quest description templates (sample data)

Quest `Description` strings in `Models` use **snake_case** logical placeholder paths for localization (`kill_count`, `weapons`, `currencies`, `player.gold_balance`). They need not match C# property names (`KillCount`, `GoldBalance`); a future quest-aware resolver may alias or bind case-insensitively.

| Scope | Convention | Example |
|---|---|---|
| Quest root | Placeholders resolve against the quest instance | `{kill_count}`, `{weapons[0]}`, `{currencies[0].name}` |
| Game root | Cross-cutting values prefix with `player.` | `{player.gold_balance}` |
