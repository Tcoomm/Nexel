namespace Nexel.Ast;

public sealed class VariableExpression : Expression
{
    public VariableExpression(AstPosition position, string name)
        : base(position)
    {
        Name = name;
    }

    public string Name { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}