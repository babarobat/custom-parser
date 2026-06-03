namespace CustomParser.Lexer;

internal static class TemplateLexer
{
    internal static IReadOnlyList<Token> Tokenize(string template)
    {
        if (template.Length == 0)
            return Array.Empty<Token>();

        var tokens = new List<Token>();
        var literal = new System.Text.StringBuilder();
        var i = 0;

        void FlushLiteral()
        {
            if (literal.Length == 0)
                return;
            tokens.Add(new Token(TokenKind.Literal, literal.ToString(), i - literal.Length));
            literal.Clear();
        }

        while (i < template.Length)
        {
            var ch = template[i];

            if (ch == '{')
            {
                if (i + 1 < template.Length && template[i + 1] == '{')
                {
                    literal.Append('{');
                    i += 2;
                    continue;
                }

                FlushLiteral();
                var start = i;
                i++;
                var content = new System.Text.StringBuilder();
                var closed = false;

                while (i < template.Length)
                {
                    if (template[i] == '{')
                        throw new TemplateParseException("Unescaped '{' inside placeholder.", i);

                    if (template[i] == '}')
                    {
                        tokens.Add(new Token(TokenKind.Placeholder, content.ToString(), start));
                        i++;
                        closed = true;
                        break;
                    }

                    content.Append(template[i]);
                    i++;
                }

                if (!closed)
                    throw new TemplateParseException("Unclosed placeholder.", start);

                continue;
            }

            if (ch == '}')
            {
                if (i + 1 < template.Length && template[i + 1] == '}')
                {
                    literal.Append('}');
                    i += 2;
                    continue;
                }

                throw new TemplateParseException("Unescaped '}' in template.", i);
            }

            literal.Append(ch);
            i++;
        }

        FlushLiteral();
        return tokens;
    }
}
