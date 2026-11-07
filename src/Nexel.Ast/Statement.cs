namespace Nexel.Ast;

public abstract class Statement : AstNode
{
    protected Statement(AstPosition position)
        : base(position)
    {
    }
}