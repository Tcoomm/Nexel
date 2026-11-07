namespace Nexel.Ast;

public sealed class BlockStatement : Statement
{
    public BlockStatement(AstPosition position, IReadOnlyList<Statement> statements)
        : base(position)
    {
        Statements = statements;
    }

    public IReadOnlyList<Statement> Statements { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}