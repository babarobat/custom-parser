namespace custom_parser.Resolver;

public interface IValueProvider
{
    bool TryGetValue(string key, out object? value);

    bool TryGetIndex(object? target, int index, out object? value);

    bool TryGetMember(object? target, string member, out object? value);
}
