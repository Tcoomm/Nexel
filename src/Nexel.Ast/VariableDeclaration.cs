namespace Nexel.Ast;

public sealed class VariableDeclaration : Declaration
{
    public VariableDeclaration(AstPosition position, LangType type, string name, Expression initializer)
        : base(position)
    {
        Type = type;
        Name = name;
        Initializer = initializer;
    }

    public LangType Type { get; }

    public string Name { get; }

    public Expression Initializer { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}