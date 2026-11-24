namespace Nexel.Ast;

public sealed class DoubleLiteralExpression : Expression
{
    public DoubleLiteralExpression(AstPosition position, double value)
        : base(position)
    {
        Value = value;
    }

    public double Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}