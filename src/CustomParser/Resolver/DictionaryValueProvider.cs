namespace CustomParser.Resolver;

public sealed class DictionaryValueProvider : IValueProvider
{
    private readonly IReadOnlyDictionary<string, object?> _data;

    public DictionaryValueProvider(IReadOnlyDictionary<string, object?> data) =>
        _data = data;

    public bool TryGetValue(string key, out object? value) =>
        _data.TryGetValue(key, out value);

    public bool TryGetIndex(object? target, int index, out object? value) =>
        MemberAccessor.TryGetIndex(target, index, out value);

    public bool TryGetMember(object? target, string member, out object? value) =>
        MemberAccessor.TryGetMember(target, member, out value);
}
