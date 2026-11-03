namespace Nexel.Ast;

public abstract class Expression : AstNode
{
    protected Expression(AstPosition position)
        : base(position)
    {
    }
}