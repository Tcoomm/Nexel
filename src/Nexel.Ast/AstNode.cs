namespace Nexel.Ast;

public abstract class AstNode
{
    protected AstNode(AstPosition position)
    {
        Position = position;
    }

    public AstPosition Position { get; }

    public abstract void Accept(IAstVisitor visitor);
}