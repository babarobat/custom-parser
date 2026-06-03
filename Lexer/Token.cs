namespace custom_parser.Lexer;

internal enum TokenKind
{
    Literal,
    Placeholder,
}

internal readonly record struct Token(TokenKind Kind, string Value, int Position);
