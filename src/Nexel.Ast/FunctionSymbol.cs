namespace Nexel.Ast;

public sealed class FunctionSymbol
{
    public FunctionSymbol(string name, LangType returnType, IReadOnlyList<LangType> parameterTypes)
    {
        Name = name;
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
    }

    public string Name { get; }

    public LangType ReturnType { get; }

    public IReadOnlyList<LangType> ParameterTypes { get; }
}