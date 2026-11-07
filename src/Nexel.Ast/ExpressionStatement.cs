namespace Nexel.Ast;

public sealed class ExpressionStatement : Statement
{
    public ExpressionStatement(AstPosition position, Expression expression)
        : base(position)
    {
        Expression = expression;
    }

    public Expression Expression { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}