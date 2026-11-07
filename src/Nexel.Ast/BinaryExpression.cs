namespace Nexel.Ast;

public sealed class BinaryExpression : Expression
{
    public BinaryExpression(AstPosition position, BinaryOperator op, Expression left, Expression right)
        : base(position)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public BinaryOperator Operator { get; }

    public Expression Left { get; }

    public Expression Right { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}