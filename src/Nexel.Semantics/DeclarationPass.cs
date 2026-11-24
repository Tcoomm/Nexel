namespace Nexel.Semantics;

using Nexel.Ast;

internal sealed class DeclarationPass : AbstractPass
{
    private readonly Dictionary<string, VariableSymbol> _globals = new Dictionary<string, VariableSymbol>(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionSymbol> _functions = new Dictionary<string, FunctionSymbol>(StringComparer.Ordinal)
    {
        ["readInt"] = new FunctionSymbol("readInt", LangType.Int, Array.Empty<LangType>()),
        ["print"] = new FunctionSymbol("print", LangType.Void, new[] { LangType.Int }),
        ["abs"] = new FunctionSymbol("abs", LangType.Int, new[] { LangType.Int }),
        ["min"] = new FunctionSymbol("min", LangType.Int, new[] { LangType.Int, LangType.Int }),
        ["max"] = new FunctionSymbol("max", LangType.Int, new[] { LangType.Int, LangType.Int }),
    };

    public IReadOnlyDictionary<string, VariableSymbol> Globals => _globals;

    public IReadOnlyDictionary<string, FunctionSymbol> Functions => _functions;

    public override void Visit(ProgramNode node)
    {
        base.Visit(node);

        if (!_functions.TryGetValue("main", out FunctionSymbol? main))
        {
            throw new SemanticException("Program must define 'void main()'", node.Position);
        }

        node.MainFunction = main;
    }

    public override void Visit(VariableDeclaration node)
    {
        EnsureNameAvailable(node.Name, node.Position);

        if (node.Type != LangType.Int && node.Type != LangType.Bool)
        {
            throw new SemanticException("Global variable must have type int or bool", node.Position);
        }

        VariableSymbol symbol = new VariableSymbol(node.Name, node.Type);
        _globals.Add(symbol.Name, symbol);
        node.Symbol = symbol;
    }

    public override void Visit(FunctionDeclaration node)
    {
        EnsureNameAvailable(node.Name, node.Position);

        if (node.ReturnType != LangType.Int && node.ReturnType != LangType.Bool && node.ReturnType != LangType.Void)
        {
            throw new SemanticException("Invalid function return type", node.Position);
        }

        if (node.Name == "main" && (node.ReturnType != LangType.Void || node.Parameters.Count != 0))
        {
            throw new SemanticException("Function 'main' must have signature 'void main()'", node.Position);
        }

        FunctionSymbol symbol = new FunctionSymbol(node.Name, node.ReturnType, node.Parameters.Select(parameter => parameter.Type).ToArray());
        _functions.Add(symbol.Name, symbol);
        node.Symbol = symbol;
    }

    private void EnsureNameAvailable(string name, AstPosition position)
    {
        if (_globals.ContainsKey(name) || _functions.ContainsKey(name))
        {
            throw new SemanticException($"Duplicate or conflicting top-level declaration '{name}'", position);
        }
    }
}