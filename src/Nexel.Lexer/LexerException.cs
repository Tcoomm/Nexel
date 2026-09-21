namespace Nexel.Lexer;

public sealed class LexerException : Exception
{
    public LexerException()
    {
    }

    public LexerException(string message)
        : base(message)
    {
    }

    public LexerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LexerException(string message, SourcePosition position)
        : base($"{message} at {position}")
    {
        Position = position;
    }

    public SourcePosition Position { get; }
}