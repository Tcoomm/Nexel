namespace Nexel.Ast;

public sealed class VariableDeclarationStatement : Statement
{
    public VariableDeclarationStatement(AstPosition position, VariableDeclaration declaration)
        : base(position)
    {
        Declaration = declaration;
    }

    public VariableDeclaration Declaration { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}