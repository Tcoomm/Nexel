namespace Nexel.Lexer;

public sealed class Token
{
    public Token(TokenKind kind, string lexeme, SourcePosition position)
    {
        Kind = kind;
        Lexeme = lexeme;
        Position = position;
    }

    public TokenKind Kind { get; }

    public string Lexeme { get; }

    public SourcePosition Position { get; }

    public override string ToString() => $"{Kind}('{Lexeme}') at {Position}";
}