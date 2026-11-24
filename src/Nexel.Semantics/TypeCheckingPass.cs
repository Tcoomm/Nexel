namespace Nexel.Semantics;

using Nexel.Ast;

internal sealed class TypeCheckingPass : AbstractPass
{
    private readonly IReadOnlyDictionary<string, VariableSymbol> _globals;
    private readonly IReadOnlyDictionary<string, FunctionSymbol> _functions;
    private readonly Stack<Dictionary<string, VariableSymbol>> _scopes = new Stack<Dictionary<string, VariableSymbol>>();

    private FunctionDeclaration? _currentFunction;

    public TypeCheckingPass(DeclarationPass declarations)
    {
        _globals = declarations.Globals;
        _functions = declarations.Functions;
    }

    public override void Visit(FunctionDeclaration node)
    {
        _currentFunction = node;
        PushScope();

        foreach (ParameterNode parameter in node.Parameters)
        {
            parameter.Accept(this);
        }

        // Параметры и внешний блок функции образуют одну область видимости.
        base.Visit(node.Body);
        _scopes.Pop();
        _currentFunction = null;
    }

    public override void Visit(BlockStatement node)
    {
        PushScope();
        base.Visit(node);
        _scopes.Pop();
    }

    public override void Visit(VariableDeclaration node)
    {
        RequireType(node.Initializer, node.Type);

        if (_currentFunction is not null)
        {
            // Инициализатор проверяется до объявления новой переменной.
            node.Symbol = DeclareLocal(node.Name, node.Type, node.Position);
        }
    }

    public override void Visit(ParameterNode node)
    {
        node.Symbol = DeclareLocal(node.Name, node.Type, node.Position);
    }

    public override void Visit(AssignmentStatement node)
    {
        VariableSymbol symbol = ResolveVariable(node.Name, node.Position);
        RequireType(node.Value, symbol.Type);
        node.Symbol = symbol;
    }

    public override void Visit(ReturnStatement node)
    {
        FunctionDeclaration function = _currentFunction
            ?? throw new SemanticException("Return statement is only allowed inside a function", node.Position);

        if (function.ReturnType == LangType.Void)
        {
            if (node.Value is not null)
            {
                throw new SemanticException($"Void function '{function.Name}' cannot return a value", node.Position);
            }
        }
        else
        {
            if (node.Value is null)
            {
                throw new SemanticException($"Function '{function.Name}' must return a value", node.Position);
            }

            RequireType(node.Value, function.ReturnType);
        }
    }

    public override void Visit(IntegerLiteralExpression node)
    {
        node.ResultType = LangType.Int;
    }

    public override void Visit(VariableExpression node)
    {
        if (_currentFunction is null)
        {
            throw new SemanticException("Variable cannot be used in a global initializer", node.Position);
        }

        VariableSymbol symbol = ResolveVariable(node.Name, node.Position);
        node.Symbol = symbol;
        node.ResultType = symbol.Type;
    }

    public override void Visit(UnaryExpression node)
    {
        RequireType(node.Operand, LangType.Int);
        node.ResultType = LangType.Int;
    }

    public override void Visit(BinaryExpression node)
    {
        RequireType(node.Left, LangType.Int);
        RequireType(node.Right, LangType.Int);
        node.ResultType = LangType.Int;
    }

    public override void Visit(CallExpression node)
    {
        if (_currentFunction is null)
        {
            throw new SemanticException("Function call is not allowed in a global initializer", node.Position);
        }

        if (node.Name == "main")
        {
            throw new SemanticException("Explicit call to 'main' is forbidden", node.Position);
        }

        if (!_functions.TryGetValue(node.Name, out FunctionSymbol? function))
        {
            throw new SemanticException($"Unknown function '{node.Name}'", node.Position);
        }

        if (node.Arguments.Count != function.ParameterTypes.Count)
        {
            throw new SemanticException(
                $"Function '{node.Name}' expects {function.ParameterTypes.Count} argument(s), got {node.Arguments.Count}",
                node.Position);
        }

        for (int i = 0; i < node.Arguments.Count; i++)
        {
            RequireType(node.Arguments[i], function.ParameterTypes[i]);
        }

        node.Symbol = function;
        node.ResultType = function.ReturnType;
    }

    private void PushScope()
    {
        _scopes.Push(new Dictionary<string, VariableSymbol>(StringComparer.Ordinal));
    }

    private VariableSymbol DeclareLocal(string name, LangType type, AstPosition position)
    {
        if (type != LangType.Int)
        {
            throw new SemanticException("Variable or parameter must have type int", position);
        }

        if (_functions.ContainsKey(name))
        {
            throw new SemanticException($"Local variable or parameter '{name}' cannot use a function name", position);
        }

        VariableSymbol symbol = new VariableSymbol(name, type);
        if (!_scopes.Peek().TryAdd(symbol.Name, symbol))
        {
            throw new SemanticException($"Duplicate variable or parameter declaration '{name}' in the same scope", position);
        }

        return symbol;
    }

    private VariableSymbol ResolveVariable(string name, AstPosition position)
    {
        foreach (Dictionary<string, VariableSymbol> scope in _scopes)
        {
            if (scope.TryGetValue(name, out VariableSymbol? local))
            {
                return local;
            }
        }

        if (_globals.TryGetValue(name, out VariableSymbol? global))
        {
            return global;
        }

        throw new SemanticException($"Unknown variable '{name}'", position);
    }

    private void RequireType(Expression expression, LangType expected)
    {
        expression.Accept(this);
        if (expression.ResultType != expected)
        {
            throw new SemanticException($"Expected {expected} expression, got {expression.ResultType}", expression.Position);
        }
    }
}