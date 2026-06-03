namespace CustomParser.Parser;

internal static class ExpressionParser
{
    internal static ParsedPlaceholder Parse(string placeholderContent, int position)
    {
        var colonIndex = placeholderContent.IndexOf(':');
        string expressionPart;
        string? format;

        if (colonIndex >= 0)
        {
            expressionPart = placeholderContent[..colonIndex];
            format = placeholderContent[(colonIndex + 1)..];
        }
        else
        {
            expressionPart = placeholderContent;
            format = null;
        }

        expressionPart = expressionPart.Trim();
        format = format?.Trim();
        if (format?.Length == 0)
            format = null;

        if (expressionPart.Length == 0)
            throw new TemplateParseException("Empty placeholder expression.", position);

        return new ParsedPlaceholder(ParseAccessPath(expressionPart, position), format);
    }

    private static AccessPathNode ParseAccessPath(string expression, int position)
    {
        var index = 0;
        var root = ReadIdentifier(expression, ref index, position);

        var segments = new List<AccessSegment>(4);
        while (index < expression.Length)
        {
            if (expression[index] == '.')
            {
                index++;
                segments.Add(new MemberSegment(ReadIdentifier(expression, ref index, position)));
            }
            else if (expression[index] == '[')
            {
                index++;
                segments.Add(new IndexSegment(ReadIndex(expression, ref index, position)));
                if (index >= expression.Length || expression[index] != ']')
                    throw new TemplateParseException("Expected ']' after index.", position + index);
                index++;
            }
            else
            {
                throw new TemplateParseException($"Unexpected character '{expression[index]}' in expression.", position + index);
            }
        }

        return new AccessPathNode(root, segments);
    }

    private static string ReadIdentifier(string expression, ref int index, int position)
    {
        if (index >= expression.Length || !IsIdentifierStart(expression[index]))
            throw new TemplateParseException("Expected identifier.", position + index);

        var start = index;
        index++;
        while (index < expression.Length && IsIdentifierPart(expression[index]))
            index++;

        return expression[start..index];
    }

    private static int ReadIndex(string expression, ref int index, int position)
    {
        var start = index;
        if (index >= expression.Length || !char.IsDigit(expression[index]))
            throw new TemplateParseException("Expected non-negative integer index.", position + index);

        while (index < expression.Length && char.IsDigit(expression[index]))
            index++;

        var text = expression[start..index];
        if (!int.TryParse(text, out var value) || value < 0)
            throw new TemplateParseException("Index must be a non-negative integer.", position + start);

        return value;
    }

    private static bool IsIdentifierStart(char ch) =>
        ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';

    private static bool IsIdentifierPart(char ch) =>
        IsIdentifierStart(ch) || ch is >= '0' and <= '9';
}
