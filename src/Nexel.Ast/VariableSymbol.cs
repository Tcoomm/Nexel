namespace Nexel.Ast;

public sealed class VariableSymbol
{
    public VariableSymbol(string name, LangType type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }

    public LangType Type { get; }
}