using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace CustomParser.Resolver;

public static class MemberAccessor
{
    private static readonly ConcurrentDictionary<(Type Type, string Member), PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, string> SnakeToPascalCache = new(StringComparer.Ordinal);

    public static bool TryGetIndex(object? target, int index, out object? value)
    {
        value = null;
        if (target is null)
            return false;

        if (target is IList list)
        {
            if (index < 0 || index >= list.Count)
                return false;
            value = list[index];
            return true;
        }

        return false;
    }

    public static bool TryGetMember(object? target, string member, out object? value)
    {
        value = null;
        if (target is null)
            return false;

        if (target is IDictionary<string, object?> dict)
            return dict.TryGetValue(member, out value);

        var property = GetCachedProperty(target.GetType(), member);
        if (property is null)
            return false;

        value = property.GetValue(target);
        return true;
    }

    private static PropertyInfo? GetCachedProperty(Type type, string member) =>
        PropertyCache.GetOrAdd((type, member), static key => FindProperty(key.Type, key.Member));

    private static PropertyInfo? FindProperty(Type type, string member)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        var exact = type.GetProperty(member, flags);
        if (exact is not null)
            return exact;

        var pascal = ToPascalCase(member);
        if (!string.Equals(pascal, member, StringComparison.Ordinal))
            return type.GetProperty(pascal, flags);

        return null;
    }

    private static string ToPascalCase(string snake) =>
        SnakeToPascalCache.GetOrAdd(snake, static s =>
        {
            if (s.Length == 0)
                return s;

            var parts = s.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return s;

            var builder = new StringBuilder(s.Length);
            foreach (var part in parts)
            {
                if (part.Length == 0)
                    continue;
                builder.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    builder.Append(part.AsSpan(1));
            }

            return builder.ToString();
        });
}
