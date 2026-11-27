namespace Nexel.VmCodegen.UnitTests;

using Nexel.Ast;
using Nexel.Semantics;
using Nexel.Vm;
using Nexel.VmCodegen;

public sealed class ControlFlowCodegenTests
{
    private static readonly AstPosition Pos = new AstPosition(1, 1);

    [Fact]
    public void Generate_BooleanLiteral_EmitsPushBool()
    {
        VmProgram program = GenerateMain(
            new VariableDeclarationStatement(Pos, new VariableDeclaration(Pos, LangType.Bool, "b", new BooleanLiteralExpression(Pos, true))));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.PushBool, actual[0].Code);
        Assert.Equal(1, actual[0].Operand);
        Assert.Equal(OpCode.StoreLocal, actual[1].Code);
        Assert.Equal(OpCode.Return, actual[2].Code);
    }

    [Fact]
    public void Generate_IfWithoutElse_EmitsJumpIfFalse()
    {
        VmProgram program = GenerateMain(
            new IfStatement(
                Pos,
                new BooleanLiteralExpression(Pos, false),
                new BlockStatement(Pos, new Statement[]
                {
                    new ExpressionStatement(Pos, new CallExpression(Pos, "print", new Expression[] { new IntegerLiteralExpression(Pos, 1) })),
                }),
                elseBlock: null));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.PushBool, actual[0].Code);
        Assert.Equal(OpCode.JumpIfFalse, actual[1].Code);
        Assert.Equal(OpCode.Push, actual[2].Code);
        Assert.Equal(OpCode.Print, actual[3].Code);
        Assert.Equal(OpCode.Return, actual[4].Code);
        Assert.InRange(actual[1].Operand, 2, program.Instructions.Count);
    }

    [Fact]
    public void Generate_IfWithElse_EmitsJumpToEnd()
    {
        VmProgram program = GenerateMain(
            new IfStatement(
                Pos,
                new BooleanLiteralExpression(Pos, true),
                new BlockStatement(Pos, new Statement[]
                {
                    new ExpressionStatement(Pos, new CallExpression(Pos, "print", new Expression[] { new IntegerLiteralExpression(Pos, 1) })),
                }),
                new BlockStatement(Pos, new Statement[]
                {
                    new ExpressionStatement(Pos, new CallExpression(Pos, "print", new Expression[] { new IntegerLiteralExpression(Pos, 0) })),
                })));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.PushBool, actual[0].Code);
        Assert.Equal(OpCode.JumpIfFalse, actual[1].Code);
        Assert.Equal(OpCode.Push, actual[2].Code);
        Assert.Equal(OpCode.Print, actual[3].Code);
        Assert.Equal(OpCode.Jump, actual[4].Code);
        Assert.Equal(OpCode.Push, actual[5].Code);
        Assert.Equal(OpCode.Print, actual[6].Code);
        Assert.Equal(OpCode.Return, actual[7].Code);
        Assert.InRange(actual[1].Operand, 5, program.Instructions.Count);
        Assert.InRange(actual[4].Operand, 7, program.Instructions.Count);
    }

    [Fact]
    public void Generate_WhileLoop_EmitsBackwardJump()
    {
        VmProgram program = GenerateMain(
            new WhileStatement(
                Pos,
                new BinaryExpression(Pos, BinaryOperator.Greater, new IntegerLiteralExpression(Pos, 0), new IntegerLiteralExpression(Pos, 1)),
                new BlockStatement(Pos, Array.Empty<Statement>())));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.Push, actual[0].Code);
        Assert.Equal(OpCode.Push, actual[1].Code);
        Assert.Equal(OpCode.Greater, actual[2].Code);
        Assert.Equal(OpCode.JumpIfFalse, actual[3].Code);
        Assert.Equal(OpCode.Jump, actual[4].Code);
        Assert.Equal(OpCode.Return, actual[5].Code);
        Assert.Equal(main.EntryPoint, actual[4].Operand);
    }

    [Fact]
    public void Generate_ShortCircuitAnd_EmitsJumpIfFalseWithFalseFallback()
    {
        VmProgram program = GenerateMain(
            new VariableDeclarationStatement(
                Pos,
                new VariableDeclaration(
                    Pos,
                    LangType.Bool,
                    "b",
                    new BinaryExpression(
                        Pos,
                        BinaryOperator.And,
                        new BooleanLiteralExpression(Pos, true),
                        new BooleanLiteralExpression(Pos, false)))));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.PushBool, actual[0].Code);
        Assert.Equal(OpCode.JumpIfFalse, actual[1].Code);
        Assert.Equal(OpCode.PushBool, actual[2].Code);
        Assert.Equal(OpCode.Jump, actual[3].Code);
        Assert.Equal(OpCode.PushBool, actual[4].Code);
        Assert.Equal(0, actual[4].Operand);
        Assert.Equal(OpCode.StoreLocal, actual[5].Code);
        Assert.Equal(OpCode.Return, actual[6].Code);
    }

    [Fact]
    public void Generate_ShortCircuitOr_EmitsJumpIfTrueWithTrueFallback()
    {
        VmProgram program = GenerateMain(
            new VariableDeclarationStatement(
                Pos,
                new VariableDeclaration(
                    Pos,
                    LangType.Bool,
                    "b",
                    new BinaryExpression(
                        Pos,
                        BinaryOperator.Or,
                        new BooleanLiteralExpression(Pos, false),
                        new BooleanLiteralExpression(Pos, true)))));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.PushBool, actual[0].Code);
        Assert.Equal(OpCode.JumpIfTrue, actual[1].Code);
        Assert.Equal(OpCode.PushBool, actual[2].Code);
        Assert.Equal(OpCode.Jump, actual[3].Code);
        Assert.Equal(OpCode.PushBool, actual[4].Code);
        Assert.Equal(1, actual[4].Operand);
        Assert.Equal(OpCode.StoreLocal, actual[5].Code);
        Assert.Equal(OpCode.Return, actual[6].Code);
    }

    [Fact]
    public void Generate_Comparison_EmitsComparisonOpCode()
    {
        VmProgram program = GenerateMain(
            new VariableDeclarationStatement(
                Pos,
                new VariableDeclaration(
                    Pos,
                    LangType.Bool,
                    "b",
                    new BinaryExpression(Pos, BinaryOperator.Less, new IntegerLiteralExpression(Pos, 1), new IntegerLiteralExpression(Pos, 2)))));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.Push, actual[0].Code);
        Assert.Equal(1, actual[0].Operand);
        Assert.Equal(OpCode.Push, actual[1].Code);
        Assert.Equal(2, actual[1].Operand);
        Assert.Equal(OpCode.Less, actual[2].Code);
        Assert.Equal(OpCode.StoreLocal, actual[3].Code);
        Assert.Equal(OpCode.Return, actual[4].Code);
    }

    [Fact]
    public void Generate_NotOperator_EmitsNotOpCode()
    {
        VmProgram program = GenerateMain(
            new VariableDeclarationStatement(
                Pos,
                new VariableDeclaration(
                    Pos,
                    LangType.Bool,
                    "b",
                    new UnaryExpression(Pos, UnaryOperator.Not, new BooleanLiteralExpression(Pos, false)))));

        VmFunction main = program.Functions[1];
        Instruction[] actual = program.Instructions.Skip(main.EntryPoint).ToArray();

        Assert.Equal(OpCode.PushBool, actual[0].Code);
        Assert.Equal(0, actual[0].Operand);
        Assert.Equal(OpCode.Not, actual[1].Code);
        Assert.Equal(OpCode.StoreLocal, actual[2].Code);
        Assert.Equal(OpCode.Return, actual[3].Code);
    }

    private static VmProgram GenerateMain(params Statement[] statements)
    {
        FunctionDeclaration main = new FunctionDeclaration(
            Pos,
            LangType.Void,
            "main",
            Array.Empty<ParameterNode>(),
            new BlockStatement(Pos, statements));

        ProgramNode ast = new ProgramNode(Pos, new Declaration[] { main });
        SemanticAnalyzer.Analyze(ast);
        return new VmCodeGenerator().Generate(ast);
    }
}