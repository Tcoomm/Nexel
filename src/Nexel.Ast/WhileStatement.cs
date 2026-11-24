namespace Nexel.Ast;

public sealed class WhileStatement : Statement
{
    public WhileStatement(AstPosition position, Expression condition, BlockStatement body)
        : base(position)
    {
        Condition = condition;
        Body = body;
    }

    public Expression Condition { get; }

    public BlockStatement Body { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}