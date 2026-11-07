namespace Nexel.Ast;

public sealed class AssignmentStatement : Statement
{
    public AssignmentStatement(AstPosition position, string name, Expression value)
        : base(position)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public Expression Value { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}