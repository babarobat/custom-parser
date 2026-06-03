namespace custom_parser;

public sealed class TemplateParseException : Exception
{
    public TemplateParseException(string message, int position)
        : base(message)
    {
        Position = position;
    }

    public int Position { get; }
}

public sealed class TemplateRenderException : Exception
{
    public TemplateRenderException(string message, string placeholder, int position)
        : base(message)
    {
        Placeholder = placeholder;
        Position = position;
    }

    public string Placeholder { get; }

    public int Position { get; }
}
