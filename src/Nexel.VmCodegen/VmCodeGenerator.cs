namespace Nexel.VmCodegen;

using Nexel.Ast;
using Nexel.Vm;

public sealed class VmCodeGenerator : IAstVisitor
{
    private static readonly IReadOnlyDictionary<string, OpCode> Builtins = new Dictionary<string, OpCode>(StringComparer.Ordinal)
    {
        ["readInt"] = OpCode.ReadInt,
        ["print"] = OpCode.Print,
        ["readDouble"] = OpCode.ReadDouble,
        ["printDouble"] = OpCode.PrintDouble,
        ["toDouble"] = OpCode.IntToDouble,
        ["toInt"] = OpCode.DoubleToInt,
        ["abs"] = OpCode.Abs,
        ["min"] = OpCode.Min,
        ["max"] = OpCode.Max,
    };

    private readonly List<Instruction> _instructions = new List<Instruction>();
    private readonly List<double> _doubleConstants = new List<double>();
    private readonly Dictionary<long, int> _doubleConstantIndices = new Dictionary<long, int>();
    private readonly Dictionary<VariableSymbol, int> _globals = new Dictionary<VariableSymbol, int>();
    private readonly Dictionary<VariableSymbol, int> _locals = new Dictionary<VariableSymbol, int>();
    private readonly Dictionary<FunctionSymbol, int> _functionIndices = new Dictionary<FunctionSymbol, int>();
    private readonly List<VmFunction> _functions = new List<VmFunction>();

    public VmProgram Generate(ProgramNode program)
    {
        ArgumentNullException.ThrowIfNull(program);

        _instructions.Clear();
        _doubleConstants.Clear();
        _doubleConstantIndices.Clear();
        _globals.Clear();
        _locals.Clear();
        _functionIndices.Clear();
        _functions.Clear();

        program.Accept(this);

        return new VmProgram(
            _instructions.ToList(),
            _functions.ToList(),
            _globals.Count,
            0,
            _doubleConstants.ToList());
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
        VariableSymbol symbol = RequireSymbol(node.Symbol, node.Position);

        if (node.Operator == AssignmentOperator.Assign)
        {
            node.Value.Accept(this);
            EmitVariable(symbol, OpCode.StoreGlobal, OpCode.StoreLocal);
            return;
        }

        EmitVariable(symbol, OpCode.LoadGlobal, OpCode.LoadLocal);
        node.Value.Accept(this);

        OpCode opCode = node.Operator switch
        {
            AssignmentOperator.Add => OpCode.Add,
            AssignmentOperator.Subtract => OpCode.Sub,
            AssignmentOperator.Multiply => OpCode.Mul,
            AssignmentOperator.Divide => OpCode.Div,
            _ => throw new CodegenException($"Unsupported assignment operator '{node.Operator}'", node.Position),
        };

        Emit(new Instruction(opCode));
        EmitVariable(symbol, OpCode.StoreGlobal, OpCode.StoreLocal);
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

    public void Visit(IfStatement node)
    {
        node.Condition.Accept(this);
        int jumpToElse = EmitJump(OpCode.JumpIfFalse);
        node.ThenBlock.Accept(this);

        if (node.ElseBlock is null)
        {
            PatchJump(jumpToElse, _instructions.Count);
            return;
        }

        int jumpToEnd = EmitJump(OpCode.Jump);
        PatchJump(jumpToElse, _instructions.Count);
        node.ElseBlock.Accept(this);
        PatchJump(jumpToEnd, _instructions.Count);
    }

    public void Visit(WhileStatement node)
    {
        int begin = _instructions.Count;
        node.Condition.Accept(this);
        int jumpToEnd = EmitJump(OpCode.JumpIfFalse);
        node.Body.Accept(this);
        Emit(new Instruction(OpCode.Jump, begin));
        PatchJump(jumpToEnd, _instructions.Count);
    }

    public void Visit(ForStatement node)
    {
        node.Initializer.Accept(this);
        int begin = _instructions.Count;
        node.Condition.Accept(this);
        int jumpToEnd = EmitJump(OpCode.JumpIfFalse);
        node.Body.Accept(this);
        node.Step.Accept(this);
        Emit(new Instruction(OpCode.Jump, begin));
        PatchJump(jumpToEnd, _instructions.Count);
    }

    public void Visit(IntegerLiteralExpression node)
    {
        Emit(new Instruction(OpCode.Push, node.Value));
    }

    public void Visit(DoubleLiteralExpression node)
    {
        long bits = BitConverter.DoubleToInt64Bits(node.Value);
        if (!_doubleConstantIndices.TryGetValue(bits, out int index))
        {
            index = _doubleConstants.Count;
            _doubleConstants.Add(node.Value);
            _doubleConstantIndices.Add(bits, index);
        }

        Emit(new Instruction(OpCode.PushDouble, index));
    }

    public void Visit(BooleanLiteralExpression node)
    {
        Emit(new Instruction(OpCode.PushBool, node.Value ? 1 : 0));
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

            case UnaryOperator.Not:
                Emit(new Instruction(OpCode.Not));
                break;

            default:
                throw new CodegenException($"Unsupported unary operator '{node.Operator}'", node.Position);
        }
    }

    public void Visit(BinaryExpression node)
    {
        if (node.Operator == BinaryOperator.And)
        {
            EmitShortCircuitAnd(node);
            return;
        }

        if (node.Operator == BinaryOperator.Or)
        {
            EmitShortCircuitOr(node);
            return;
        }

        node.Left.Accept(this);
        node.Right.Accept(this);

        OpCode opCode = node.Operator switch
        {
            BinaryOperator.Add => OpCode.Add,
            BinaryOperator.Subtract => OpCode.Sub,
            BinaryOperator.Multiply => OpCode.Mul,
            BinaryOperator.Divide => OpCode.Div,
            BinaryOperator.Modulo => OpCode.Mod,
            BinaryOperator.Equal => OpCode.Equal,
            BinaryOperator.NotEqual => OpCode.NotEqual,
            BinaryOperator.Less => OpCode.Less,
            BinaryOperator.LessOrEqual => OpCode.LessOrEqual,
            BinaryOperator.Greater => OpCode.Greater,
            BinaryOperator.GreaterOrEqual => OpCode.GreaterOrEqual,
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

    private void EmitShortCircuitAnd(BinaryExpression node)
    {
        node.Left.Accept(this);
        int jumpIfFalse = EmitJump(OpCode.JumpIfFalse);
        node.Right.Accept(this);
        int jumpToEnd = EmitJump(OpCode.Jump);

        PatchJump(jumpIfFalse, _instructions.Count);
        Emit(new Instruction(OpCode.PushBool, 0));
        PatchJump(jumpToEnd, _instructions.Count);
    }

    private void EmitShortCircuitOr(BinaryExpression node)
    {
        node.Left.Accept(this);
        int jumpIfTrue = EmitJump(OpCode.JumpIfTrue);
        node.Right.Accept(this);
        int jumpToEnd = EmitJump(OpCode.Jump);

        PatchJump(jumpIfTrue, _instructions.Count);
        Emit(new Instruction(OpCode.PushBool, 1));
        PatchJump(jumpToEnd, _instructions.Count);
    }

    private int EmitJump(OpCode opCode)
    {
        _instructions.Add(new Instruction(opCode));
        return _instructions.Count - 1;
    }

    private void PatchJump(int instructionIndex, int target)
    {
        _instructions[instructionIndex] = _instructions[instructionIndex] with { Operand = target };
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