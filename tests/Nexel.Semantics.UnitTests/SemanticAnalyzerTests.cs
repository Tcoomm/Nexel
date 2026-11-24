namespace Nexel.Semantics.UnitTests;

using Nexel.Ast;
using Nexel.Semantics;

public sealed class SemanticAnalyzerTests
{
    private static readonly AstPosition Pos = new AstPosition(1, 1);

    [Fact]
    public void Analyze_UnknownVariable_ThrowsSemanticExceptionWithPosition()
    {
        AstPosition errorPos = new AstPosition(7, 13);
        ProgramNode ast = MainProgram(ExprStmt(Call("print", new VariableExpression(errorPos, "missing"))));

        SemanticException exception = Assert.Throws<SemanticException>(() => SemanticAnalyzer.Analyze(ast));

        Assert.Equal(errorPos, exception.Position);
    }

    [Fact]
    public void Analyze_SelfReferencingInitializer_ThrowsSemanticExceptionWithPosition()
    {
        AstPosition errorPos = new AstPosition(7, 13);
        ProgramNode ast = MainProgram(
            new VariableDeclarationStatement(
                errorPos,
                new VariableDeclaration(errorPos, LangType.Int, "x", Bin(BinaryOperator.Add, new VariableExpression(errorPos, "x"), Lit(1)))));

        SemanticException exception = Assert.Throws<SemanticException>(() => SemanticAnalyzer.Analyze(ast));

        Assert.Equal(errorPos, exception.Position);
    }

    [Fact]
    public void Analyze_VariableConflictingWithBuiltin_ThrowsSemanticExceptionWithPosition()
    {
        AstPosition errorPos = new AstPosition(7, 13);
        ProgramNode ast = MainProgram(
            new VariableDeclarationStatement(
                errorPos,
                new VariableDeclaration(errorPos, LangType.Int, "print", Lit(1))));

        SemanticException exception = Assert.Throws<SemanticException>(() => SemanticAnalyzer.Analyze(ast));

        Assert.Equal(errorPos, exception.Position);
    }

    [Fact]
    public void Analyze_InvalidGlobalInitializer_ThrowsSemanticExceptionWithPosition()
    {
        AstPosition errorPos = new AstPosition(7, 13);
        ProgramNode ast = new ProgramNode(
            Pos,
            new Declaration[]
            {
                new VariableDeclaration(Pos, LangType.Int, "g", new CallExpression(errorPos, "readInt", Array.Empty<Expression>())),
                new FunctionDeclaration(Pos, LangType.Void, "main", Array.Empty<ParameterNode>(), Block()),
            });

        SemanticException exception = Assert.Throws<SemanticException>(() => SemanticAnalyzer.Analyze(ast));

        Assert.Equal(errorPos, exception.Position);
    }

    [Fact]
    public void Analyze_MissingReturn_ThrowsSemanticExceptionWithPosition()
    {
        AstPosition errorPos = new AstPosition(7, 13);
        ProgramNode ast = new ProgramNode(
            Pos,
            new Declaration[]
            {
                new FunctionDeclaration(errorPos, LangType.Int, "broken", Array.Empty<ParameterNode>(), Block(ExprStmt(Call("print", Lit(1))))),
                new FunctionDeclaration(Pos, LangType.Void, "main", Array.Empty<ParameterNode>(), Block()),
            });

        SemanticException exception = Assert.Throws<SemanticException>(() => SemanticAnalyzer.Analyze(ast));

        Assert.Equal(errorPos, exception.Position);
    }

    [Fact]
    public void Analyze_ShadowedVariable_ResolvesDifferentSymbols()
    {
        VariableDeclaration outer = new VariableDeclaration(Pos, LangType.Int, "x", Lit(1));
        VariableDeclaration inner = new VariableDeclaration(Pos, LangType.Int, "x", Lit(2));
        VariableExpression innerUse = new VariableExpression(Pos, "x");
        VariableExpression outerUse = new VariableExpression(Pos, "x");
        ProgramNode ast = MainProgram(
            new VariableDeclarationStatement(Pos, outer),
            Block(new VariableDeclarationStatement(Pos, inner), ExprStmt(innerUse)),
            ExprStmt(outerUse));

        SemanticAnalyzer.Analyze(ast);

        Assert.NotNull(outer.Symbol);
        Assert.NotNull(inner.Symbol);
        Assert.NotSame(outer.Symbol, inner.Symbol);
        Assert.Same(inner.Symbol, innerUse.Symbol);
        Assert.Same(outer.Symbol, outerUse.Symbol);
    }

    [Fact]
    public void Analyze_ExpressionTypes_AreWrittenToAst()
    {
        IntegerLiteralExpression literal = Lit(2);
        BinaryExpression sum = Bin(BinaryOperator.Add, literal, Lit(3));
        CallExpression print = Call("print", sum);
        ProgramNode ast = MainProgram(ExprStmt(print));

        SemanticAnalyzer.Analyze(ast);

        Assert.Equal(LangType.Int, literal.ResultType);
        Assert.Equal(LangType.Int, sum.ResultType);
        Assert.Equal(LangType.Void, print.ResultType);
        Assert.NotNull(print.Symbol);
        Assert.Equal("print", print.Symbol.Name);
    }

    [Fact]
    public void Analyze_ForwardCall_UsesDeclaredFunctionSymbol()
    {
        CallExpression call = Call("answer");
        FunctionDeclaration main = new FunctionDeclaration(Pos, LangType.Void, "main", Array.Empty<ParameterNode>(), Block(ExprStmt(call)));
        FunctionDeclaration answer = new FunctionDeclaration(Pos, LangType.Int, "answer", Array.Empty<ParameterNode>(), Block(new ReturnStatement(Pos, Lit(42))));
        ProgramNode ast = new ProgramNode(Pos, new Declaration[] { main, answer });

        SemanticAnalyzer.Analyze(ast);

        Assert.NotNull(answer.Symbol);
        Assert.Same(answer.Symbol, call.Symbol);
        Assert.Equal(LangType.Int, call.ResultType);
    }

    private static ProgramNode MainProgram(params Statement[] statements)
    {
        FunctionDeclaration main = new FunctionDeclaration(Pos, LangType.Void, "main", Array.Empty<ParameterNode>(), Block(statements));
        return new ProgramNode(Pos, new Declaration[] { main });
    }

    private static BlockStatement Block(params Statement[] statements) =>
        new BlockStatement(Pos, statements);

    private static ExpressionStatement ExprStmt(Expression expression) =>
        new ExpressionStatement(Pos, expression);

    private static IntegerLiteralExpression Lit(int value) =>
        new IntegerLiteralExpression(Pos, value);

    private static BinaryExpression Bin(BinaryOperator op, Expression left, Expression right) =>
        new BinaryExpression(Pos, op, left, right);

    private static CallExpression Call(string name, params Expression[] args) =>
        new CallExpression(Pos, name, args);
}