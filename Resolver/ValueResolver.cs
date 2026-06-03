using custom_parser.Parser;

namespace custom_parser.Resolver;

internal enum ResolveOutcome
{
    Success,
    Missing,
}

internal readonly record struct ResolveResult(object? Value, ResolveOutcome Outcome);

internal static class ValueResolver
{
    internal static ResolveResult Resolve(AstNode node, IValueProvider context)
    {
        if (node is not AccessPathNode path)
            return new ResolveResult(null, ResolveOutcome.Missing);

        if (!context.TryGetValue(path.Root, out var current))
            return new ResolveResult(null, ResolveOutcome.Missing);

        foreach (var segment in path.Segments)
        {
            var ok = segment switch
            {
                MemberSegment member => context.TryGetMember(current, member.Name, out current),
                IndexSegment index => context.TryGetIndex(current, index.Index, out current),
                _ => false,
            };

            if (!ok)
                return new ResolveResult(null, ResolveOutcome.Missing);
        }

        return new ResolveResult(current, ResolveOutcome.Success);
    }
}
