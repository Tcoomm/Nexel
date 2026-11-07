namespace Nexel.Ast;

public sealed class IntegerLiteralExpression : Expression
{
    public IntegerLiteralExpression(AstPosition position, int value)
        : base(position)
    {
        Value = value;
    }

    public int Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}