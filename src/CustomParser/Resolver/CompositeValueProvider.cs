namespace CustomParser.Resolver;

public sealed class CompositeValueProvider : IValueProvider
{
    private readonly IReadOnlyList<IValueProvider> _providers;

    public CompositeValueProvider(params IValueProvider[] providers) =>
        _providers = providers;

    public CompositeValueProvider(IReadOnlyList<IValueProvider> providers) =>
        _providers = providers;

    public bool TryGetValue(string key, out object? value)
    {
        foreach (var provider in _providers)
        {
            if (provider.TryGetValue(key, out value))
                return true;
        }

        value = null;
        return false;
    }

    public bool TryGetIndex(object? target, int index, out object? value)
    {
        foreach (var provider in _providers)
        {
            if (provider.TryGetIndex(target, index, out value))
                return true;
        }

        value = null;
        return false;
    }

    public bool TryGetMember(object? target, string member, out object? value)
    {
        foreach (var provider in _providers)
        {
            if (provider.TryGetMember(target, member, out value))
                return true;
        }

        value = null;
        return false;
    }
}
