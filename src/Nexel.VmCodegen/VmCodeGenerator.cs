namespace Nexel.VmCodegen;

using Nexel.Ast;
using Nexel.Vm;

public sealed class VmCodeGenerator : IAstVisitor
{
    private static readonly IReadOnlyDictionary<string, BuiltinFunctionInfo> Builtins =
        new Dictionary<string, BuiltinFunctionInfo>(StringComparer.Ordinal)
        {
            ["readInt"] = new BuiltinFunctionInfo("readInt", 0, LangType.Int, OpCode.ReadInt),
            ["print"] = new BuiltinFunctionInfo("print", 1, LangType.Void, OpCode.Print),
            ["abs"] = new BuiltinFunctionInfo("abs", 1, LangType.Int, OpCode.Abs),
            ["min"] = new BuiltinFunctionInfo("min", 2, LangType.Int, OpCode.Min),
            ["max"] = new BuiltinFunctionInfo("max", 2, LangType.Int, OpCode.Max),
        };

    private readonly List<Instruction> _instructions = new List<Instruction>();
    private readonly Dictionary<string, int> _globals = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionMetadata> _functions = new Dictionary<string, FunctionMetadata>(StringComparer.Ordinal);
    private readonly List<FunctionMetadata> _orderedFunctions = new List<FunctionMetadata>();
    private readonly HashSet<string> _reservedFunctionNames = new HashSet<string>(StringComparer.Ordinal);
    private readonly Stack<Dictionary<string, int>> _scopes = new Stack<Dictionary<string, int>>();

    private FunctionMetadata? _currentFunction;
    private int _currentLocalCount;
    private bool _inGlobalInitializer;
    private bool _isFunctionRootBlock;
    private LangType _lastExpressionType;

    public VmProgram Generate(ProgramNode program)
    {
        ArgumentNullException.ThrowIfNull(program);

        ResetState();
        program.Accept(this);

        List<VmFunction> vmFunctions = _orderedFunctions
            .Select(f => new VmFunction(
                Name: f.Name,
                EntryPoint: f.EntryPoint,
                ParameterCount: f.ParameterCount,
                LocalCount: f.LocalCount,
                ReturnsValue: f.ReturnsValue))
            .ToList();

        return new VmProgram(
            Instructions: _instructions.ToList(),
            Functions: vmFunctions,
            GlobalCount: _globals.Count,
            EntryFunctionIndex: 0);
    }

    public void Visit(ProgramNode node)
    {
        FunctionMetadata entryMetadata = new FunctionMetadata(
            name: "<entry>",
            index: 0,
            parameterCount: 0,
            returnType: LangType.Void,
            declaration: null);

        _orderedFunctions.Add(entryMetadata);

        List<VariableDeclaration> globalDeclarations = new List<VariableDeclaration>();
        List<FunctionDeclaration> functionDeclarations = new List<FunctionDeclaration>();

        // Проход 1: сбор и проверка имён верхнего уровня
        foreach (Declaration declaration in node.Declarations)
        {
            switch (declaration)
            {
                case VariableDeclaration globalVar:
                    if (globalVar.Type != LangType.Int)
                    {
                        throw new CodegenException(
                            $"Global variable '{globalVar.Name}' must have type int",
                            globalVar.Position);
                    }

                    EnsureTopLevelNameAvailable(globalVar.Name, globalVar.Position);
                    int slot = _globals.Count;
                    _globals[globalVar.Name] = slot;
                    globalDeclarations.Add(globalVar);
                    break;

                case FunctionDeclaration funcDecl:
                    EnsureTopLevelNameAvailable(funcDecl.Name, funcDecl.Position);
                    int funcIndex = _orderedFunctions.Count;
                    FunctionMetadata metadata = new FunctionMetadata(
                        name: funcDecl.Name,
                        index: funcIndex,
                        parameterCount: funcDecl.Parameters.Count,
                        returnType: funcDecl.ReturnType,
                        declaration: funcDecl);

                    _functions[funcDecl.Name] = metadata;
                    _orderedFunctions.Add(metadata);
                    _reservedFunctionNames.Add(funcDecl.Name);
                    functionDeclarations.Add(funcDecl);
                    break;

                default:
                    throw new CodegenException(
                        $"Unsupported declaration node '{declaration.GetType().Name}'",
                        declaration.Position);
            }
        }

        if (!_functions.TryGetValue("main", out FunctionMetadata? mainFunction))
        {
            throw new CodegenException("Program must define 'void main()'", node.Position);
        }

        if (mainFunction.ReturnType != LangType.Void || mainFunction.ParameterCount != 0)
        {
            AstPosition mainPos = mainFunction.Declaration?.Position ?? node.Position;
            throw new CodegenException("Function 'main' must have signature 'void main()'", mainPos);
        }

        // Проход 2: стартовая функция <entry> (глобальные инициализаторы -> Call main -> Return)
        entryMetadata.EntryPoint = _instructions.Count;
        entryMetadata.LocalCount = 0;

        foreach (VariableDeclaration globalVar in globalDeclarations)
        {
            globalVar.Accept(this);
        }

        Emit(new Instruction(OpCode.Call, mainFunction.Index));
        Emit(new Instruction(OpCode.Return));

        // Генерация тел всех пользовательских функций
        foreach (FunctionDeclaration funcDecl in functionDeclarations)
        {
            funcDecl.Accept(this);
        }
    }

    public void Visit(VariableDeclaration node)
    {
        if (_currentFunction is null)
        {
            _inGlobalInitializer = true;
            try
            {
                EvaluateIntExpression(node.Initializer);
            }
            finally
            {
                _inGlobalInitializer = false;
            }

            int globalSlot = _globals[node.Name];
            Emit(new Instruction(OpCode.StoreGlobal, globalSlot));
            return;
        }

        if (node.Type != LangType.Int)
        {
            throw new CodegenException(
                $"Local variable '{node.Name}' must have type int",
                node.Position);
        }

        // Инициализатор проверяется ДО добавления имени новой переменной в область видимости
        EvaluateIntExpression(node.Initializer);
        int localSlot = DeclareLocal(node.Name, node.Position);
        Emit(new Instruction(OpCode.StoreLocal, localSlot));
    }

    public void Visit(FunctionDeclaration node)
    {
        FunctionMetadata metadata = _functions[node.Name];
        _currentFunction = metadata;
        _currentLocalCount = 0;
        _scopes.Clear();

        metadata.EntryPoint = _instructions.Count;

        // Параметры и внешний блок функции разделяют одну область видимости
        PushScope();

        foreach (ParameterNode parameter in node.Parameters)
        {
            parameter.Accept(this);
        }

        _isFunctionRootBlock = true;
        node.Body.Accept(this);

        if (metadata.ReturnsValue)
        {
            if (!GuaranteesReturn(node.Body))
            {
                throw new CodegenException(
                    $"Function '{node.Name}' with return type int must guarantee a return statement",
                    node.Position);
            }
        }
        else
        {
            if (_instructions.Count == metadata.EntryPoint || _instructions[^1].Code != OpCode.Return)
            {
                Emit(new Instruction(OpCode.Return));
            }
        }

        PopScope();
        metadata.LocalCount = _currentLocalCount;
        _currentFunction = null;
    }

    public void Visit(ParameterNode node)
    {
        if (node.Type != LangType.Int)
        {
            throw new CodegenException(
                $"Parameter '{node.Name}' must have type int",
                node.Position);
        }

        DeclareLocal(node.Name, node.Position);
    }

    public void Visit(BlockStatement node)
    {
        bool isRoot = _isFunctionRootBlock;
        _isFunctionRootBlock = false;

        if (!isRoot)
        {
            PushScope();
        }

        foreach (Statement statement in node.Statements)
        {
            statement.Accept(this);
        }

        if (!isRoot)
        {
            PopScope();
        }
    }

    public void Visit(VariableDeclarationStatement node)
    {
        node.Declaration.Accept(this);
    }

    public void Visit(AssignmentStatement node)
    {
        EvaluateIntExpression(node.Value);

        if (TryResolveLocal(node.Name, out int localSlot))
        {
            Emit(new Instruction(OpCode.StoreLocal, localSlot));
            return;
        }

        if (_globals.TryGetValue(node.Name, out int globalSlot))
        {
            Emit(new Instruction(OpCode.StoreGlobal, globalSlot));
            return;
        }

        throw new CodegenException($"Unknown variable '{node.Name}'", node.Position);
    }

    public void Visit(ExpressionStatement node)
    {
        node.Expression.Accept(this);

        if (_lastExpressionType == LangType.Int)
        {
            Emit(new Instruction(OpCode.Pop));
        }
    }

    public void Visit(ReturnStatement node)
    {
        if (_currentFunction is null)
        {
            throw new CodegenException("Return statement is only allowed inside a function", node.Position);
        }

        if (_currentFunction.ReturnsValue)
        {
            if (node.Value is null)
            {
                throw new CodegenException(
                    $"Function '{_currentFunction.Name}' must return an int value",
                    node.Position);
            }

            EvaluateIntExpression(node.Value);
            Emit(new Instruction(OpCode.Return));
            return;
        }

        if (node.Value is not null)
        {
            throw new CodegenException(
                $"Void function '{_currentFunction.Name}' cannot return a value",
                node.Position);
        }

        Emit(new Instruction(OpCode.Return));
    }

    public void Visit(IntegerLiteralExpression node)
    {
        Emit(new Instruction(OpCode.Push, node.Value));
        _lastExpressionType = LangType.Int;
    }

    public void Visit(VariableExpression node)
    {
        if (_inGlobalInitializer)
        {
            throw new CodegenException(
                $"Variable '{node.Name}' cannot be used in a global variable initializer",
                node.Position);
        }

        if (TryResolveLocal(node.Name, out int localSlot))
        {
            Emit(new Instruction(OpCode.LoadLocal, localSlot));
            _lastExpressionType = LangType.Int;
            return;
        }

        if (_globals.TryGetValue(node.Name, out int globalSlot))
        {
            Emit(new Instruction(OpCode.LoadGlobal, globalSlot));
            _lastExpressionType = LangType.Int;
            return;
        }

        throw new CodegenException($"Unknown variable '{node.Name}'", node.Position);
    }

    public void Visit(UnaryExpression node)
    {
        EvaluateIntExpression(node.Operand);

        switch (node.Operator)
        {
            case UnaryOperator.Plus:
                break;

            case UnaryOperator.Negate:
                Emit(new Instruction(OpCode.Neg));
                break;

            default:
                throw new CodegenException($"Unsupported unary operator '{node.Operator}'", node.Position);
        }

        _lastExpressionType = LangType.Int;
    }

    public void Visit(BinaryExpression node)
    {
        EvaluateIntExpression(node.Left);
        EvaluateIntExpression(node.Right);

        OpCode opCode = node.Operator switch
        {
            BinaryOperator.Add => OpCode.Add,
            BinaryOperator.Subtract => OpCode.Sub,
            BinaryOperator.Multiply => OpCode.Mul,
            BinaryOperator.Divide => OpCode.Div,
            BinaryOperator.Modulo => OpCode.Mod,
            _ => throw new CodegenException($"Unsupported binary operator '{node.Operator}'", node.Position),
        };

        Emit(new Instruction(opCode));
        _lastExpressionType = LangType.Int;
    }

    public void Visit(CallExpression node)
    {
        if (_inGlobalInitializer)
        {
            throw new CodegenException(
                $"Function call '{node.Name}' is not allowed in a global variable initializer",
                node.Position);
        }

        if (string.Equals(node.Name, "main", StringComparison.Ordinal))
        {
            throw new CodegenException("Explicit call to 'main' is forbidden", node.Position);
        }

        if (TryResolveLocal(node.Name, out _) || _globals.ContainsKey(node.Name))
        {
            throw new CodegenException($"Variable '{node.Name}' cannot be called as a function", node.Position);
        }

        if (Builtins.TryGetValue(node.Name, out BuiltinFunctionInfo? builtin))
        {
            if (node.Arguments.Count != builtin.ParameterCount)
            {
                throw new CodegenException(
                    $"Function '{node.Name}' expects {builtin.ParameterCount} argument(s), got {node.Arguments.Count}",
                    node.Position);
            }

            foreach (Expression arg in node.Arguments)
            {
                EvaluateIntExpression(arg);
            }

            Emit(new Instruction(builtin.OpCode));
            _lastExpressionType = builtin.ReturnType;
            return;
        }

        if (_functions.TryGetValue(node.Name, out FunctionMetadata? targetFunction))
        {
            if (node.Arguments.Count != targetFunction.ParameterCount)
            {
                throw new CodegenException(
                    $"Function '{node.Name}' expects {targetFunction.ParameterCount} argument(s), got {node.Arguments.Count}",
                    node.Position);
            }

            foreach (Expression arg in node.Arguments)
            {
                EvaluateIntExpression(arg);
            }

            Emit(new Instruction(OpCode.Call, targetFunction.Index));
            _lastExpressionType = targetFunction.ReturnType;
            return;
        }

        throw new CodegenException($"Unknown function '{node.Name}'", node.Position);
    }

    private void ResetState()
    {
        _instructions.Clear();
        _globals.Clear();
        _functions.Clear();
        _orderedFunctions.Clear();
        _reservedFunctionNames.Clear();
        _scopes.Clear();

        foreach (string builtinName in Builtins.Keys)
        {
            _reservedFunctionNames.Add(builtinName);
        }

        _reservedFunctionNames.Add("main");

        _currentFunction = null;
        _currentLocalCount = 0;
        _inGlobalInitializer = false;
        _isFunctionRootBlock = false;
        _lastExpressionType = LangType.Void;
    }

    private void EnsureTopLevelNameAvailable(string name, AstPosition position)
    {
        if (Builtins.ContainsKey(name))
        {
            throw new CodegenException($"Name '{name}' is reserved for a built-in function", position);
        }

        if (_globals.ContainsKey(name) || _functions.ContainsKey(name))
        {
            throw new CodegenException($"Duplicate or conflicting top-level declaration '{name}'", position);
        }
    }

    private void PushScope()
    {
        _scopes.Push(new Dictionary<string, int>(StringComparer.Ordinal));
    }

    private void PopScope()
    {
        _scopes.Pop();
    }

    private int DeclareLocal(string name, AstPosition position)
    {
        if (_reservedFunctionNames.Contains(name))
        {
            throw new CodegenException(
                $"Local variable or parameter '{name}' cannot use a function name",
                position);
        }

        Dictionary<string, int> currentScope = _scopes.Peek();
        if (currentScope.ContainsKey(name))
        {
            throw new CodegenException(
                $"Duplicate variable or parameter declaration '{name}' in the same scope",
                position);
        }

        int slot = _currentLocalCount++;
        currentScope[name] = slot;
        return slot;
    }

    private bool TryResolveLocal(string name, out int slot)
    {
        foreach (Dictionary<string, int> scope in _scopes)
        {
            if (scope.TryGetValue(name, out slot))
            {
                return true;
            }
        }

        slot = -1;
        return false;
    }

    private void EvaluateIntExpression(Expression expression)
    {
        expression.Accept(this);

        if (_lastExpressionType != LangType.Int)
        {
            throw new CodegenException(
                "Void expression cannot be used where an int value is required",
                expression.Position);
        }
    }

    private static bool GuaranteesReturn(BlockStatement block)
    {
        foreach (Statement statement in block.Statements)
        {
            if (statement is ReturnStatement)
            {
                return true;
            }

            if (statement is BlockStatement nestedBlock && GuaranteesReturn(nestedBlock))
            {
                return true;
            }
        }

        return false;
    }

    private void Emit(Instruction instruction)
    {
        _instructions.Add(instruction);
    }

    private sealed record BuiltinFunctionInfo(
        string Name,
        int ParameterCount,
        LangType ReturnType,
        OpCode OpCode);

    private sealed class FunctionMetadata
    {
        public FunctionMetadata(
            string name,
            int index,
            int parameterCount,
            LangType returnType,
            FunctionDeclaration? declaration)
        {
            Name = name;
            Index = index;
            ParameterCount = parameterCount;
            ReturnType = returnType;
            Declaration = declaration;
        }

        public string Name { get; }

        public int Index { get; }

        public int ParameterCount { get; }

        public LangType ReturnType { get; }

        public bool ReturnsValue => ReturnType == LangType.Int;

        public FunctionDeclaration? Declaration { get; }

        public int EntryPoint { get; set; }

        public int LocalCount { get; set; }
    }
}