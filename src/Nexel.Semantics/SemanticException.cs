namespace Nexel.Semantics;

using Nexel.Ast;

public sealed class SemanticException : Exception
{
    public SemanticException()
    {
    }

    public SemanticException(string message)
        : base(message)
    {
    }

    public SemanticException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SemanticException(string message, AstPosition position)
        : base($"{message} at ({position.Line}, {position.Column})")
    {
        Position = position;
    }

    public AstPosition Position { get; }
}