namespace CustomParser.Lexer;

internal enum TokenKind
{
    Literal,
    Placeholder,
}

internal readonly record struct Token(TokenKind Kind, string Value, int Position);
