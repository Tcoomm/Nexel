namespace Nexel.Ast;

public sealed class CallExpression : Expression
{
    public CallExpression(AstPosition position, string name, IReadOnlyList<Expression> arguments)
        : base(position)
    {
        Name = name;
        Arguments = arguments;
    }

    public string Name { get; }

    public IReadOnlyList<Expression> Arguments { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}