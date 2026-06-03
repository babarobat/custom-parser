using System.Collections;

namespace CustomParser.Resolver;

public static class MemberAccessor
{
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

        return false;
    }
}
