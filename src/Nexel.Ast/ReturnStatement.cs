namespace Nexel.Ast;

public sealed class ReturnStatement : Statement
{
    public ReturnStatement(AstPosition position, Expression? value)
        : base(position)
    {
        Value = value;
    }

    public Expression? Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}