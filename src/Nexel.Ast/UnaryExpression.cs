namespace Nexel.Ast;

public sealed class UnaryExpression : Expression
{
    public UnaryExpression(AstPosition position, UnaryOperator op, Expression operand)
        : base(position)
    {
        Operator = op;
        Operand = operand;
    }

    public UnaryOperator Operator { get; }

    public Expression Operand { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}