namespace Nexel.Ast;

public sealed class ForStatement : Statement
{
    public ForStatement(
        AstPosition position,
        Statement initializer,
        Expression condition,
        AssignmentStatement step,
        BlockStatement body)
        : base(position)
    {
        Initializer = initializer;
        Condition = condition;
        Step = step;
        Body = body;
    }

    public Statement Initializer { get; }

    public Expression Condition { get; }

    public AssignmentStatement Step { get; }

    public BlockStatement Body { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}