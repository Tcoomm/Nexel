namespace Nexel.VmCodegen.UnitTests;

using Nexel.Ast;
using Nexel.Vm;
using Nexel.VmCodegen;

public sealed class CodegenTests
{
    private static readonly AstPosition Pos = new AstPosition(1, 1);

    [Fact]
    public void Generate_EmptyMain_EmitsEntryAndMainWithImplicitReturn()
    {
        ProgramNode ast = MainProgram();

        VmProgram program = new VmCodeGenerator().Generate(ast);

        Assert.Equal(0, program.EntryFunctionIndex);
        Assert.Equal(0, program.GlobalCount);
        Assert.Equal(2, program.Functions.Count);

        VmFunction entry = program.Functions[0];
        VmFunction main = program.Functions[1];

        Assert.Equal("main", main.Name);
        Assert.Equal(0, main.ParameterCount);
        Assert.Equal(0, main.LocalCount);
        Assert.False(main.ReturnsValue);

        Instruction[] expected =
        {
            new Instruction(OpCode.Call, 1),
            new Instruction(OpCode.Return),
            new Instruction(OpCode.Return),
        };

        Assert.Equal(expected, program.Instructions);
        Assert.Equal(0, entry.EntryPoint);
        Assert.Equal(2, main.EntryPoint);
    }

    [Fact]
    public void Generate_ArithmeticPrecedence_10Plus2Mul5_EmitsPushMulAdd()
    {
        Expression expr = Bin(
            BinaryOperator.Add,
            Lit(10),
            Bin(BinaryOperator.Multiply, Lit(2), Lit(5)));

        AssertMainInstructions(
            new[] { ExprStmt(Call("print", expr)) },
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Mul),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));
    }

    [Fact]
    public void Generate_AllLegacyArithmeticAndBuiltins_EmitExpectedInstructions()
    {
        Expression expr = Bin(
            BinaryOperator.Add,
            Unary(UnaryOperator.Negate, Bin(BinaryOperator.Divide, Lit(20), Lit(5))),
            Unary(UnaryOperator.Plus, Bin(BinaryOperator.Modulo, Lit(17), Lit(5))));

        AssertMainInstructions(
            new[]
            {
                ExprStmt(Call("print", expr)),
                ExprStmt(Call("print", Call("readInt"))),
            },
            new Instruction(OpCode.Push, 20),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Div),
            new Instruction(OpCode.Neg),
            new Instruction(OpCode.Push, 17),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Mod),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.ReadInt),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));
    }

    [Fact]
    public void Generate_IntExpressionStatement_EmitsPop_WhileVoidCallDoesNot()
    {
        FunctionDeclaration notify = new FunctionDeclaration(
            Pos,
            LangType.Void,
            "notify",
            Array.Empty<ParameterNode>(),
            Block(new ReturnStatement(Pos, null)));

        FunctionDeclaration main = new FunctionDeclaration(
            Pos,
            LangType.Void,
            "main",
            Array.Empty<ParameterNode>(),
            Block(
                ExprStmt(Bin(BinaryOperator.Add, Lit(7), Lit(3))),
                ExprStmt(Call("notify"))));

        ProgramNode ast = new ProgramNode(Pos, new Declaration[] { notify, main });
        VmProgram vmProgram = new VmCodeGenerator().Generate(ast);

        VmFunction mainFn = vmProgram.Functions[2];
        IReadOnlyList<Instruction> mainBody = vmProgram.Instructions.Skip(mainFn.EntryPoint).ToList();

        Instruction[] expectedMain =
        {
            new Instruction(OpCode.Push, 7),
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Pop),
            new Instruction(OpCode.Call, 1),
            new Instruction(OpCode.Return),
        };

        Assert.Equal(expectedMain, mainBody);
    }

    [Fact]
    public void Generate_SameVariableNameInNestedBlocks_GetsDistinctLocalSlots()
    {
        Statement[] body =
        {
            LocalVar("x", Lit(1)),
            Block(
                LocalVar("x", Lit(2)),
                ExprStmt(Call("print", Var("x")))),
            ExprStmt(Call("print", Var("x"))),
        };

        VmProgram program = new VmCodeGenerator().Generate(MainProgram(body));
        VmFunction main = program.Functions[1];

        Assert.Equal(2, main.LocalCount);

        Instruction[] expectedMain =
        {
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.StoreLocal, 0),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.StoreLocal, 1),
            new Instruction(OpCode.LoadLocal, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.LoadLocal, 0),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return),
        };

        Assert.Equal(expectedMain, program.Instructions.Skip(main.EntryPoint));
    }

    [Fact]
    public void Generate_GlobalInitializers_EmittedInEntryBeforeCallMain()
    {
        VariableDeclaration g1 = new VariableDeclaration(Pos, LangType.Int, "first", Bin(BinaryOperator.Add, Lit(40), Lit(2)));
        VariableDeclaration g2 = new VariableDeclaration(Pos, LangType.Int, "second", Lit(10));
        FunctionDeclaration main = new FunctionDeclaration(
            Pos,
            LangType.Void,
            "main",
            Array.Empty<ParameterNode>(),
            Block(ExprStmt(Call("print", Var("first")))));

        ProgramNode ast = new ProgramNode(Pos, new Declaration[] { g1, main, g2 });
        VmProgram program = new VmCodeGenerator().Generate(ast);

        Assert.Equal(2, program.GlobalCount);

        Instruction[] expectedEntry =
        {
            new Instruction(OpCode.Push, 40),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.StoreGlobal, 0),
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.StoreGlobal, 1),
            new Instruction(OpCode.Call, 1), // индекс main = 1
            new Instruction(OpCode.Return),
        };

        VmFunction mainFn = program.Functions[1];
        Assert.Equal(expectedEntry, program.Instructions.Take(mainFn.EntryPoint));
    }

    [Fact]
    public void Generate_ForwardFunctionCall_ResolvesIndexEntryPointAndLocalCount()
    {
        FunctionDeclaration main = new FunctionDeclaration(
            Pos,
            LangType.Void,
            "main",
            Array.Empty<ParameterNode>(),
            Block(ExprStmt(Call("print", Call("add", Lit(2), Lit(3))))));

        FunctionDeclaration add = new FunctionDeclaration(
            Pos,
            LangType.Int,
            "add",
            new[]
            {
                new ParameterNode(Pos, LangType.Int, "a"),
                new ParameterNode(Pos, LangType.Int, "b"),
            },
            Block(
                LocalVar("sum", Bin(BinaryOperator.Add, Var("a"), Var("b"))),
                new ReturnStatement(Pos, Var("sum"))));

        VmProgram program = new VmCodeGenerator().Generate(new ProgramNode(Pos, new Declaration[] { main, add }));

        VmFunction addFn = program.Functions[2];
        Assert.Equal("add", addFn.Name);
        Assert.Equal(2, addFn.ParameterCount);
        Assert.Equal(3, addFn.LocalCount);
        Assert.True(addFn.ReturnsValue);

        Instruction[] expectedAdd =
        {
            new Instruction(OpCode.LoadLocal, 0),
            new Instruction(OpCode.LoadLocal, 1),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.StoreLocal, 2),
            new Instruction(OpCode.LoadLocal, 2),
            new Instruction(OpCode.Return),
        };

        Assert.Equal(expectedAdd, program.Instructions.Skip(addFn.EntryPoint));
    }

    [Fact]
    public void Generate_BuiltinAbsMinMax_EmitsDedicatedOpCodes()
    {
        Statement[] body =
        {
            ExprStmt(Call("print", Call("abs", Lit(-5)))),
            ExprStmt(Call("print", Call("min", Lit(3), Lit(7)))),
            ExprStmt(Call("print", Call("max", Lit(3), Lit(7)))),
        };

        AssertMainInstructions(
            body,
            new Instruction(OpCode.Push, -5),
            new Instruction(OpCode.Abs),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Push, 7),
            new Instruction(OpCode.Min),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Push, 7),
            new Instruction(OpCode.Max),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));
    }

    [Fact]
    public void Generate_SemanticErrors_ThrowCodegenExceptionWithPosition()
    {
        AstPosition errorPos = new AstPosition(7, 13);

        ProgramNode unknownVar = MainProgram(ExprStmt(Call("print", new VariableExpression(errorPos, "missing"))));
        CodegenException ex1 = Assert.Throws<CodegenException>(() => new VmCodeGenerator().Generate(unknownVar));
        Assert.Equal(errorPos, ex1.Position);

        ProgramNode selfInit = MainProgram(
            new VariableDeclarationStatement(
                errorPos,
                new VariableDeclaration(errorPos, LangType.Int, "x", Bin(BinaryOperator.Add, new VariableExpression(errorPos, "x"), Lit(1)))));
        CodegenException ex2 = Assert.Throws<CodegenException>(() => new VmCodeGenerator().Generate(selfInit));
        Assert.Equal(errorPos, ex2.Position);

        ProgramNode shadowFunc = MainProgram(
            new VariableDeclarationStatement(
                errorPos,
                new VariableDeclaration(errorPos, LangType.Int, "print", Lit(1))));
        CodegenException ex3 = Assert.Throws<CodegenException>(() => new VmCodeGenerator().Generate(shadowFunc));
        Assert.Equal(errorPos, ex3.Position);

        ProgramNode invalidGlobal = new ProgramNode(
            Pos,
            new Declaration[]
            {
                new VariableDeclaration(Pos, LangType.Int, "g", new CallExpression(errorPos, "readInt", Array.Empty<Expression>())),
                new FunctionDeclaration(Pos, LangType.Void, "main", Array.Empty<ParameterNode>(), Block()),
            });
        CodegenException ex4 = Assert.Throws<CodegenException>(() => new VmCodeGenerator().Generate(invalidGlobal));
        Assert.Equal(errorPos, ex4.Position);

        ProgramNode missingReturn = new ProgramNode(
            Pos,
            new Declaration[]
            {
                new FunctionDeclaration(errorPos, LangType.Int, "broken", Array.Empty<ParameterNode>(), Block(ExprStmt(Call("print", Lit(1))))),
                new FunctionDeclaration(Pos, LangType.Void, "main", Array.Empty<ParameterNode>(), Block()),
            });
        CodegenException ex5 = Assert.Throws<CodegenException>(() => new VmCodeGenerator().Generate(missingReturn));
        Assert.Equal(errorPos, ex5.Position);
    }

    private static void AssertMainInstructions(IReadOnlyList<Statement> mainStatements, params Instruction[] expectedMain)
    {
        ProgramNode ast = MainProgram(mainStatements.ToArray());
        VmProgram program = new VmCodeGenerator().Generate(ast);
        VmFunction main = program.Functions[1];

        IReadOnlyList<Instruction> actualMain = program.Instructions.Skip(main.EntryPoint).ToList();
        Assert.Equal(expectedMain, actualMain);
    }

    private static ProgramNode MainProgram(params Statement[] statements)
    {
        FunctionDeclaration main = new FunctionDeclaration(
            Pos,
            LangType.Void,
            "main",
            Array.Empty<ParameterNode>(),
            new BlockStatement(Pos, statements));

        return new ProgramNode(Pos, new Declaration[] { main });
    }

    private static BlockStatement Block(params Statement[] statements) =>
        new BlockStatement(Pos, statements);

    private static VariableDeclarationStatement LocalVar(string name, Expression init) =>
        new VariableDeclarationStatement(Pos, new VariableDeclaration(Pos, LangType.Int, name, init));

    private static ExpressionStatement ExprStmt(Expression expr) =>
        new ExpressionStatement(Pos, expr);

    private static IntegerLiteralExpression Lit(int value) =>
        new IntegerLiteralExpression(Pos, value);

    private static VariableExpression Var(string name) =>
        new VariableExpression(Pos, name);

    private static UnaryExpression Unary(UnaryOperator op, Expression operand) =>
        new UnaryExpression(Pos, op, operand);

    private static BinaryExpression Bin(BinaryOperator op, Expression left, Expression right) =>
        new BinaryExpression(Pos, op, left, right);

    private static CallExpression Call(string name, params Expression[] args) =>
        new CallExpression(Pos, name, args);
}
