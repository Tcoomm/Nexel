namespace Nexel.Ast;

public sealed class BooleanLiteralExpression : Expression
{
    public BooleanLiteralExpression(AstPosition position, bool value)
        : base(position)
    {
        Value = value;
    }

    public bool Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}