namespace Nexel.Ast;

public sealed class ParameterNode : AstNode
{
    public ParameterNode(AstPosition position, LangType type, string name)
        : base(position)
    {
        Type = type;
        Name = name;
    }

    public LangType Type { get; }

    public string Name { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}