namespace Nexel.Ast;

public sealed class AssignmentStatement : Statement
{
    public AssignmentStatement(
        AstPosition position,
        string name,
        Expression value,
        AssignmentOperator op = AssignmentOperator.Assign)
        : base(position)
    {
        Name = name;
        Value = value;
        Operator = op;
    }

    public string Name { get; }

    public Expression Value { get; }

    public AssignmentOperator Operator { get; }

    public VariableSymbol? Symbol { get; set; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}