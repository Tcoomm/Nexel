namespace Nexel.Ast;

public sealed class ProgramNode : AstNode
{
    public ProgramNode(AstPosition position, IReadOnlyList<Declaration> declarations)
        : base(position)
    {
        Declarations = declarations;
    }

    public IReadOnlyList<Declaration> Declarations { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}