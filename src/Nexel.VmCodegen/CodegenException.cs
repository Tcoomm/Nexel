namespace Nexel.VmCodegen;

using Nexel.Ast;

public sealed class CodegenException : Exception
{
    public CodegenException()
    {
    }

    public CodegenException(string message)
        : base(message)
    {
    }

    public CodegenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public CodegenException(string message, AstPosition position)
        : base($"{message} at ({position.Line}, {position.Column})")
    {
        Position = position;
    }

    public AstPosition Position { get; }
}
