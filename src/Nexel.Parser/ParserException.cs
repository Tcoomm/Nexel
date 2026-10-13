namespace Nexel.Parser;

using Nexel.Lexer;

public sealed class ParserException : Exception
{
    public ParserException()
    {
    }

    public ParserException(string message)
        : base(message)
    {
    }

    public ParserException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ParserException(string message, SourcePosition position)
        : base($"{message} at {position}")
    {
        Position = position;
    }

    public SourcePosition Position { get; }
}