namespace Nexel.VmCodegen;

using Nexel.Ast;
using Nexel.Vm;

public sealed class VmCodeGenerator : IAstVisitor
{
    private static readonly IReadOnlyDictionary<string, OpCode> Builtins = new Dictionary<string, OpCode>(StringComparer.Ordinal)
    {
        ["readInt"] = OpCode.ReadInt,
        ["print"] = OpCode.Print,
        ["abs"] = OpCode.Abs,
        ["min"] = OpCode.Min,
        ["max"] = OpCode.Max,
    };

    private readonly List<Instruction> _instructions = new List<Instruction>();
    private readonly Dictionary<VariableSymbol, int> _globals = new Dictionary<VariableSymbol, int>();
    private readonly Dictionary<VariableSymbol, int> _locals = new Dictionary<VariableSymbol, int>();
    private readonly Dictionary<FunctionSymbol, int> _functionIndices = new Dictionary<FunctionSymbol, int>();
    private readonly List<VmFunction> _functions = new List<VmFunction>();

    public VmProgram Generate(ProgramNode program)
    {
        ArgumentNullException.ThrowIfNull(program);

        _instructions.Clear();
        _globals.Clear();
        _locals.Clear();
        _functionIndices.Clear();
        _functions.Clear();

        program.Accept(this);

        return new VmProgram(_instructions.ToList(), _functions.ToList(), _globals.Count, 0);
    }

    public void Visit(ProgramNode node)
    {
        FunctionSymbol main = RequireSymbol(node.MainFunction, node.Position);
        List<FunctionDeclaration> functions = node.Declarations.OfType<FunctionDeclaration>().ToList();
        for (int i = 0; i < functions.Count; i++)
        {
            _functionIndices.Add(RequireSymbol(functions[i].Symbol, functions[i].Position), i + 1);
        }

        foreach (VariableDeclaration global in node.Declarations.OfType<VariableDeclaration>())
        {
            VariableSymbol symbol = RequireSymbol(global.Symbol, global.Position);
            _globals.Add(symbol, _globals.Count);
            global.Accept(this);
        }

        Emit(new Instruction(OpCode.Call, _functionIndices[main]));
        Emit(new Instruction(OpCode.Return));
        _functions.Add(new VmFunction("<entry>", 0, 0, 0, false));

        foreach (FunctionDeclaration function in functions)
        {
            function.Accept(this);
        }
    }

    public void Visit(VariableDeclaration node)
    {
        VariableSymbol symbol = RequireSymbol(node.Symbol, node.Position);
        node.Initializer.Accept(this);

        if (!_globals.ContainsKey(symbol))
        {
            _locals.Add(symbol, _locals.Count);
        }

        EmitVariable(symbol, OpCode.StoreGlobal, OpCode.StoreLocal);
    }

    public void Visit(FunctionDeclaration node)
    {
        _locals.Clear();
        int entryPoint = _instructions.Count;

        foreach (ParameterNode parameter in node.Parameters)
        {
            parameter.Accept(this);
        }

        node.Body.Accept(this);

        bool returnsValue = node.ReturnType != LangType.Void;
        if (!returnsValue && (_instructions.Count == entryPoint || _instructions[^1].Code != OpCode.Return))
        {
            Emit(new Instruction(OpCode.Return));
        }

        _functions.Add(new VmFunction(node.Name, entryPoint, node.Parameters.Count, _locals.Count, returnsValue));
    }

    public void Visit(ParameterNode node)
    {
        _locals.Add(RequireSymbol(node.Symbol, node.Position), _locals.Count);
    }

    public void Visit(BlockStatement node)
    {
        foreach (Statement statement in node.Statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(VariableDeclarationStatement node)
    {
        node.Declaration.Accept(this);
    }

    public void Visit(AssignmentStatement node)
    {
        node.Value.Accept(this);
        EmitVariable(RequireSymbol(node.Symbol, node.Position), OpCode.StoreGlobal, OpCode.StoreLocal);
    }

    public void Visit(ExpressionStatement node)
    {
        node.Expression.Accept(this);
        if (node.Expression.ResultType != LangType.Void)
        {
            Emit(new Instruction(OpCode.Pop));
        }
    }

    public void Visit(ReturnStatement node)
    {
        node.Value?.Accept(this);
        Emit(new Instruction(OpCode.Return));
    }

    public void Visit(IntegerLiteralExpression node)
    {
        Emit(new Instruction(OpCode.Push, node.Value));
    }

    public void Visit(VariableExpression node)
    {
        EmitVariable(RequireSymbol(node.Symbol, node.Position), OpCode.LoadGlobal, OpCode.LoadLocal);
    }

    public void Visit(UnaryExpression node)
    {
        node.Operand.Accept(this);

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
    }

    public void Visit(BinaryExpression node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);

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
    }

    public void Visit(CallExpression node)
    {
        FunctionSymbol function = RequireSymbol(node.Symbol, node.Position);
        EmitArguments(node.Arguments);

        if (Builtins.TryGetValue(function.Name, out OpCode opCode))
        {
            Emit(new Instruction(opCode));
        }
        else
        {
            Emit(new Instruction(OpCode.Call, _functionIndices[function]));
        }
    }

    private static T RequireSymbol<T>(T? symbol, AstPosition position)
        where T : class
    {
        return symbol ?? throw new CodegenException("Missing semantic attribute", position);
    }

    private void EmitVariable(VariableSymbol symbol, OpCode globalOpCode, OpCode localOpCode)
    {
        if (_globals.TryGetValue(symbol, out int slot))
        {
            Emit(new Instruction(globalOpCode, slot));
        }
        else
        {
            Emit(new Instruction(localOpCode, _locals[symbol]));
        }
    }

    private void EmitArguments(IReadOnlyList<Expression> arguments)
    {
        foreach (Expression argument in arguments)
        {
            argument.Accept(this);
        }
    }

    private void Emit(Instruction instruction)
    {
        _instructions.Add(instruction);
    }
}