namespace Nexel.Ast;

public sealed class FunctionDeclaration : Declaration
{
    public FunctionDeclaration(AstPosition position, LangType returnType, string name, IReadOnlyList<ParameterNode> parameters, BlockStatement body)
        : base(position)
    {
        ReturnType = returnType;
        Name = name;
        Parameters = parameters;
        Body = body;
    }

    public LangType ReturnType { get; }

    public string Name { get; }

    public IReadOnlyList<ParameterNode> Parameters { get; }

    public BlockStatement Body { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}