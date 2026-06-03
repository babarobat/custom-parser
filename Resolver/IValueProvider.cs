namespace custom_parser.Resolver;

public interface IValueProvider
{
    object? GetValue(string key);

    object? GetIndex(object? target, int index);

    object? GetMember(object? target, string member);
}
