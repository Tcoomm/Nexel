namespace Nexel.Parser.UnitTests;

using System.Collections.Generic;

using Nexel.Ast;
using Nexel.Lexer;
using Nexel.Parser;

using Xunit;

public sealed class ParserTests
{
    [Fact]
    public void ParseEmptyMain()
    {
        ProgramNode program = Parse("void main() { }");
        FunctionDeclaration main = Assert.IsType<FunctionDeclaration>(Assert.Single(program.Declarations));

        Assert.Equal(new AstPosition(1, 1), program.Position);
        Assert.Equal(new AstPosition(1, 6), main.Position);
        Assert.Equal(LangType.Void, main.ReturnType);
        Assert.Equal("main", main.Name);
        Assert.Empty(main.Parameters);
        Assert.Empty(main.Body.Statements);
    }

    [Fact]
    public void ParsePrintNumber()
    {
        IntegerLiteralExpression number = Assert.IsType<IntegerLiteralExpression>(PrintArgument("print(5);"));
        Assert.Equal(5, number.Value);
    }

    public static TheoryData<string, BinaryOperator, int, int> ParseBinaryOperatorCases()
    {
        return new TheoryData<string, BinaryOperator, int, int>
        {
            { "print(2 + 3);", BinaryOperator.Add, 2, 3 },
            { "print(7 - 4);", BinaryOperator.Subtract, 7, 4 },
            { "print(3 * 4);", BinaryOperator.Multiply, 3, 4 },
            { "print(20 / 5);", BinaryOperator.Divide, 20, 5 },
            { "print(17 % 5);", BinaryOperator.Modulo, 17, 5 },
        };
    }

    [Theory]
    [MemberData(nameof(ParseBinaryOperatorCases))]
    public void ParseBinaryOperator(string source, BinaryOperator op, int left, int right)
    {
        BinaryExpression expression = Assert.IsType<BinaryExpression>(PrintArgument(source));
        Assert.Equal(op, expression.Operator);
        Assert.Equal(left, Assert.IsType<IntegerLiteralExpression>(expression.Left).Value);
        Assert.Equal(right, Assert.IsType<IntegerLiteralExpression>(expression.Right).Value);
    }

    [Fact]
    public void ParseUnaryOperatorsNestFromRight()
    {
        UnaryExpression minus = Assert.IsType<UnaryExpression>(PrintArgument("print(-+-1);"));
        UnaryExpression plus = Assert.IsType<UnaryExpression>(minus.Operand);
        UnaryExpression innerMinus = Assert.IsType<UnaryExpression>(plus.Operand);

        Assert.Equal(UnaryOperator.Negate, minus.Operator);
        Assert.Equal(UnaryOperator.Plus, plus.Operator);
        Assert.Equal(UnaryOperator.Negate, innerMinus.Operator);
        Assert.Equal(1, Assert.IsType<IntegerLiteralExpression>(innerMinus.Operand).Value);
    }

    [Fact]
    public void ParseReadIntAsCallExpression()
    {
        CallExpression call = Assert.IsType<CallExpression>(PrintArgument("print(readInt());"));
        Assert.Equal("readInt", call.Name);
        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void ParseReadIntPlusOne()
    {
        BinaryExpression add = Assert.IsType<BinaryExpression>(PrintArgument("print(readInt() + 1);"));
        Assert.Equal(BinaryOperator.Add, add.Operator);
        Assert.Equal("readInt", Assert.IsType<CallExpression>(add.Left).Name);
        Assert.Equal(1, Assert.IsType<IntegerLiteralExpression>(add.Right).Value);
    }

    [Fact]
    public void ParsePrecedenceMulBeforeAdd()
    {
        BinaryExpression add = Assert.IsType<BinaryExpression>(PrintArgument("print(10 + 2 * 5);"));
        BinaryExpression multiply = Assert.IsType<BinaryExpression>(add.Right);

        Assert.Equal(BinaryOperator.Add, add.Operator);
        Assert.Equal(10, Assert.IsType<IntegerLiteralExpression>(add.Left).Value);
        Assert.Equal(BinaryOperator.Multiply, multiply.Operator);
        Assert.Equal(2, Assert.IsType<IntegerLiteralExpression>(multiply.Left).Value);
        Assert.Equal(5, Assert.IsType<IntegerLiteralExpression>(multiply.Right).Value);
    }

    [Fact]
    public void ParseParenthesesOverridePrecedence()
    {
        BinaryExpression multiply = Assert.IsType<BinaryExpression>(PrintArgument("print((10 + 2) * 5);"));
        BinaryExpression add = Assert.IsType<BinaryExpression>(multiply.Left);

        Assert.Equal(BinaryOperator.Multiply, multiply.Operator);
        Assert.Equal(BinaryOperator.Add, add.Operator);
        Assert.Equal(5, Assert.IsType<IntegerLiteralExpression>(multiply.Right).Value);
    }

    [Fact]
    public void ParseSubtractionIsLeftAssociative()
    {
        BinaryExpression outer = Assert.IsType<BinaryExpression>(PrintArgument("print(10 - 3 - 2);"));
        BinaryExpression inner = Assert.IsType<BinaryExpression>(outer.Left);

        Assert.Equal(BinaryOperator.Subtract, outer.Operator);
        Assert.Equal(BinaryOperator.Subtract, inner.Operator);
        Assert.Equal(10, Assert.IsType<IntegerLiteralExpression>(inner.Left).Value);
        Assert.Equal(3, Assert.IsType<IntegerLiteralExpression>(inner.Right).Value);
        Assert.Equal(2, Assert.IsType<IntegerLiteralExpression>(outer.Right).Value);
    }

    [Fact]
    public void ParseExpressionStatement()
    {
        FunctionDeclaration main = ParseMain("7 + 3;");
        ExpressionStatement statement = Assert.IsType<ExpressionStatement>(Assert.Single(main.Body.Statements));

        Assert.IsType<BinaryExpression>(statement.Expression);
    }

    [Fact]
    public void ParseMultipleStatementsPreservesOrder()
    {
        FunctionDeclaration main = ParseMain("print(1); print(2);");

        Assert.Equal(2, main.Body.Statements.Count);
        ExpressionStatement first = Assert.IsType<ExpressionStatement>(main.Body.Statements[0]);
        ExpressionStatement second = Assert.IsType<ExpressionStatement>(main.Body.Statements[1]);
        CallExpression firstCall = Assert.IsType<CallExpression>(first.Expression);
        CallExpression secondCall = Assert.IsType<CallExpression>(second.Expression);

        Assert.Equal(1, Assert.IsType<IntegerLiteralExpression>(Assert.Single(firstCall.Arguments)).Value);
        Assert.Equal(2, Assert.IsType<IntegerLiteralExpression>(Assert.Single(secondCall.Arguments)).Value);
    }

    [Fact]
    public void ParseCallArgumentsPreserveOrder()
    {
        CallExpression call = Assert.IsType<CallExpression>(PrintArgument("print(sum(1, 2 + 3));"));
        Assert.Equal("sum", call.Name);
        Assert.Equal(2, call.Arguments.Count);
        Assert.Equal(1, Assert.IsType<IntegerLiteralExpression>(call.Arguments[0]).Value);
        Assert.IsType<BinaryExpression>(call.Arguments[1]);
    }

    [Fact]
    public void ParseIdentifierWithoutCallSuffixAsVariable()
    {
        VariableExpression variable = Assert.IsType<VariableExpression>(PrintArgument("print(x);"));
        Assert.Equal("x", variable.Name);
    }

    [Fact]
    public void ParseEmptyCallIsSyntacticallyValid()
    {
        FunctionDeclaration main = ParseMain("print();");
        ExpressionStatement statement = Assert.IsType<ExpressionStatement>(Assert.Single(main.Body.Statements));
        CallExpression call = Assert.IsType<CallExpression>(statement.Expression);

        Assert.Equal("print", call.Name);
        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void ParseNodesContainSourcePositions()
    {
        ProgramNode program = Parse("void main() {\n    print(1 + 2);\n}");
        FunctionDeclaration main = Assert.IsType<FunctionDeclaration>(Assert.Single(program.Declarations));
        ExpressionStatement statement = Assert.IsType<ExpressionStatement>(Assert.Single(main.Body.Statements));
        CallExpression call = Assert.IsType<CallExpression>(statement.Expression);
        BinaryExpression add = Assert.IsType<BinaryExpression>(Assert.Single(call.Arguments));

        Assert.Equal(new AstPosition(1, 1), program.Position);
        Assert.Equal(new AstPosition(1, 13), main.Body.Position);
        Assert.Equal(new AstPosition(2, 5), call.Position);
        Assert.Equal(new AstPosition(2, 13), add.Position);
        Assert.Equal(new AstPosition(2, 11), add.Left.Position);
    }

    [Fact]
    public void ParseMaxIntegerLiteral()
    {
        IntegerLiteralExpression number = Assert.IsType<IntegerLiteralExpression>(PrintArgument("print(2147483647);"));
        Assert.Equal(int.MaxValue, number.Value);
    }

    [Fact]
    public void ParseIntegerLiteralOutOfRangeThrowsWithPosition()
    {
        ParserException ex = Assert.Throws<ParserException>(
            () => Parse("void main() { print(2147483648); }"));

        Assert.Equal(1, ex.Position.Line);
        Assert.Contains("out of range", ex.Message);
    }

    [Fact]
    public void ParseMissingSemicolonThrowsWithPosition()
    {
        ParserException ex = Assert.Throws<ParserException>(
            () => Parse("void main() { print(1) }"));

        Assert.Equal(1, ex.Position.Line);
        Assert.True(ex.Position.Column > 0);
    }

    [Fact]
    public void ParseMissingClosingBraceThrows()
    {
        Assert.Throws<ParserException>(() => Parse("void main() { print(1);"));
    }

    [Fact]
    public void ParseRequiresEndOfFile()
    {
        Assert.Throws<ParserException>(() => Parse("void main() { } trailing"));
    }

    public static TheoryData<string> InvalidPrograms()
    {
        return new TheoryData<string>
        {
            "void main() { print(,); }",
            "void main() { print(1,); }",
            "void main() { print(1 + ); }",
            "void main() { print(1; }",
            "void main() { print 1); }",
            "void main() { print(1) }",
            "void main() { 1 + 2 }",
            "void main( { }",
            "void main() { print(1); } extra",
        };
    }

    [Theory]
    [MemberData(nameof(InvalidPrograms))]
    public void ParseInvalidProgramsThrows(string source)
    {
        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void ParseEmptySourceProducesEmptyProgram()
    {
        ProgramNode program = Parse(string.Empty);

        Assert.Empty(program.Declarations);
        Assert.Equal(new AstPosition(1, 1), program.Position);
    }

    [Fact]
    public void ParseGlobalVariableWithArithmeticInitializer()
    {
        ProgramNode program = Parse("int value = (2 + 3) * -4; void main() { }");
        VariableDeclaration variable = Assert.IsType<VariableDeclaration>(program.Declarations[0]);
        BinaryExpression multiply = Assert.IsType<BinaryExpression>(variable.Initializer);
        BinaryExpression add = Assert.IsType<BinaryExpression>(multiply.Left);
        UnaryExpression negate = Assert.IsType<UnaryExpression>(multiply.Right);

        Assert.Equal(LangType.Int, variable.Type);
        Assert.Equal("value", variable.Name);
        Assert.Equal(new AstPosition(1, 5), variable.Position);
        Assert.Equal(BinaryOperator.Multiply, multiply.Operator);
        Assert.Equal(BinaryOperator.Add, add.Operator);
        Assert.Equal(2, Assert.IsType<IntegerLiteralExpression>(add.Left).Value);
        Assert.Equal(3, Assert.IsType<IntegerLiteralExpression>(add.Right).Value);
        Assert.Equal(UnaryOperator.Negate, negate.Operator);
        Assert.Equal(4, Assert.IsType<IntegerLiteralExpression>(negate.Operand).Value);
    }

    [Fact]
    public void ParseDeclarationsPreserveSourceOrder()
    {
        ProgramNode program = Parse("int first = 1; void main() { } int second = 2;");

        Assert.Equal(3, program.Declarations.Count);
        Assert.Equal("first", Assert.IsType<VariableDeclaration>(program.Declarations[0]).Name);
        Assert.Equal("main", Assert.IsType<FunctionDeclaration>(program.Declarations[1]).Name);
        Assert.Equal("second", Assert.IsType<VariableDeclaration>(program.Declarations[2]).Name);
    }

    [Fact]
    public void ParseFunctionParametersAndReturnExpression()
    {
        ProgramNode program = Parse("int sum(int left, int right) { return left + right; }");
        FunctionDeclaration function = Assert.IsType<FunctionDeclaration>(Assert.Single(program.Declarations));
        ReturnStatement statement = Assert.IsType<ReturnStatement>(Assert.Single(function.Body.Statements));
        BinaryExpression result = Assert.IsType<BinaryExpression>(statement.Value);

        Assert.Equal("sum", function.Name);
        Assert.Equal(LangType.Int, function.ReturnType);
        Assert.Equal(2, function.Parameters.Count);
        Assert.Equal("left", function.Parameters[0].Name);
        Assert.Equal("right", function.Parameters[1].Name);
        Assert.Equal(new AstPosition(1, 13), function.Parameters[0].Position);
        Assert.Equal("left", Assert.IsType<VariableExpression>(result.Left).Name);
        Assert.Equal("right", Assert.IsType<VariableExpression>(result.Right).Name);
    }

    [Fact]
    public void ParseVoidFunctionWithEmptyReturn()
    {
        ProgramNode program = Parse("void notify() { return; }");
        FunctionDeclaration function = Assert.IsType<FunctionDeclaration>(Assert.Single(program.Declarations));
        ReturnStatement statement = Assert.IsType<ReturnStatement>(Assert.Single(function.Body.Statements));

        Assert.Equal(LangType.Void, function.ReturnType);
        Assert.Empty(function.Parameters);
        Assert.Null(statement.Value);
    }

    [Fact]
    public void ParseFunctionBeforeAndAfterMain()
    {
        ProgramNode program = Parse("int one() { return 1; } void main() { } int two() { return 2; }");

        Assert.Equal(3, program.Declarations.Count);
        Assert.Equal("one", Assert.IsType<FunctionDeclaration>(program.Declarations[0]).Name);
        Assert.Equal("main", Assert.IsType<FunctionDeclaration>(program.Declarations[1]).Name);
        Assert.Equal("two", Assert.IsType<FunctionDeclaration>(program.Declarations[2]).Name);
    }

    [Fact]
    public void ParseLocalDeclarationAndAssignment()
    {
        FunctionDeclaration main = ParseMain("int value = readInt(); value = value + 1; print(value);");
        VariableDeclarationStatement declaration =
            Assert.IsType<VariableDeclarationStatement>(main.Body.Statements[0]);
        AssignmentStatement assignment = Assert.IsType<AssignmentStatement>(main.Body.Statements[1]);
        ExpressionStatement print = Assert.IsType<ExpressionStatement>(main.Body.Statements[2]);
        BinaryExpression increment = Assert.IsType<BinaryExpression>(assignment.Value);

        Assert.Equal("value", declaration.Declaration.Name);
        Assert.Equal(LangType.Int, declaration.Declaration.Type);
        Assert.IsType<CallExpression>(declaration.Declaration.Initializer);
        Assert.Equal("value", assignment.Name);
        Assert.Equal("value", Assert.IsType<VariableExpression>(increment.Left).Name);
        Assert.Equal(1, Assert.IsType<IntegerLiteralExpression>(increment.Right).Value);
        Assert.IsType<CallExpression>(print.Expression);
    }

    [Fact]
    public void ParseNestedBlocksWithShadowedVariable()
    {
        FunctionDeclaration main = ParseMain("int x = 1; { int x = 2; print(x); } print(x);");
        BlockStatement nested = Assert.IsType<BlockStatement>(main.Body.Statements[1]);
        VariableDeclarationStatement inner =
            Assert.IsType<VariableDeclarationStatement>(nested.Statements[0]);
        ExpressionStatement innerPrint = Assert.IsType<ExpressionStatement>(nested.Statements[1]);
        CallExpression call = Assert.IsType<CallExpression>(innerPrint.Expression);

        Assert.Equal(3, main.Body.Statements.Count);
        Assert.Equal(2, nested.Statements.Count);
        Assert.Equal("x", inner.Declaration.Name);
        Assert.Equal("x", Assert.IsType<VariableExpression>(Assert.Single(call.Arguments)).Name);
    }

    [Fact]
    public void ParseStandaloneCallAsExpressionStatement()
    {
        ExpressionStatement statement =
            Assert.IsType<ExpressionStatement>(Assert.Single(ParseMain("notify();").Body.Statements));
        CallExpression call = Assert.IsType<CallExpression>(statement.Expression);

        Assert.Equal("notify", call.Name);
        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void ParseReturnInNestedBlock()
    {
        FunctionDeclaration main = ParseMain("{ return 7; }");
        BlockStatement block = Assert.IsType<BlockStatement>(Assert.Single(main.Body.Statements));
        ReturnStatement statement = Assert.IsType<ReturnStatement>(Assert.Single(block.Statements));

        Assert.Equal(7, Assert.IsType<IntegerLiteralExpression>(statement.Value).Value);
    }

    [Fact]
    public void ParsePositionsOfDeclarationsAndReturn()
    {
        string source = "int count = 1;\nint sum(int value) {\n    return value + 2;\n}";
        ProgramNode program = Parse(source);
        VariableDeclaration global = Assert.IsType<VariableDeclaration>(program.Declarations[0]);
        FunctionDeclaration function = Assert.IsType<FunctionDeclaration>(program.Declarations[1]);
        ReturnStatement result = Assert.IsType<ReturnStatement>(Assert.Single(function.Body.Statements));
        BinaryExpression add = Assert.IsType<BinaryExpression>(result.Value);

        Assert.Equal(new AstPosition(1, 1), program.Position);
        Assert.Equal(new AstPosition(1, 5), global.Position);
        Assert.Equal(new AstPosition(2, 5), function.Position);
        Assert.Equal(new AstPosition(2, 13), function.Parameters[0].Position);
        Assert.Equal(new AstPosition(3, 5), result.Position);
        Assert.Equal(new AstPosition(3, 18), add.Position);
    }

    public static TheoryData<string> ParseGlobalInitializerCannotUseNamesOrCallsCases()
    {
        return new TheoryData<string>
        {
            { "int value = readInt(); void main() { }" },
            { "int value = 1; int next = value + 2; void main() { }" },
            { "int value = (3 + abs(1)); void main() { }" },
        };
    }

    [Theory]
    [MemberData(nameof(ParseGlobalInitializerCannotUseNamesOrCallsCases))]
    public void ParseGlobalInitializerCannotUseNamesOrCalls(string source)
    {
        Assert.Throws<ParserException>(() => Parse(source));
    }

    public static TheoryData<string> ParseMalformedVariablesFunctionsOrBlocksThrowsCases()
    {
        return new TheoryData<string>
        {
            { "int value; void main() { }" },
            { "void value = 1; void main() { }" },
            { "int value = ; void main() { }" },
            { "int sum(int a,) { return a; }" },
            { "int sum(void a) { return 1; }" },
            { "int sum(int a int b) { return a; }" },
            { "void main() { int x; }" },
            { "void main() { x = ; }" },
            { "void main() { int x = 1; x = x = 2; }" },
            { "void main() { return 1 }" },
            { "void main() { { return; }" },
            { "void main() { int nested() { return 1; } }" },
            { "int value = 1; garbage" },
        };
    }

    [Theory]
    [MemberData(nameof(ParseMalformedVariablesFunctionsOrBlocksThrowsCases))]
    public void ParseMalformedVariablesFunctionsOrBlocksThrows(string source)
    {
        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void ParseDuplicateFunctionsWithoutSemanticValidation()
    {
        ProgramNode program = Parse("void main() { } void main() { }");

        Assert.Equal(2, program.Declarations.Count);
    }

    [Fact]
    public void ParseUnknownFunctionWithoutSemanticValidation()
    {
        FunctionDeclaration main = ParseMain("missing(1);");
        ExpressionStatement statement = Assert.IsType<ExpressionStatement>(Assert.Single(main.Body.Statements));

        Assert.Equal("missing", Assert.IsType<CallExpression>(statement.Expression).Name);
    }

    private static ProgramNode Parse(string source)
    {
        IReadOnlyList<Token> tokens = new Lexer(source).Tokenize();
        return new global::Nexel.Parser.Parser(tokens).ParseProgram();
    }

    private static FunctionDeclaration ParseMain(string body)
    {
        ProgramNode program = Parse("void main() { " + body + " }");
        return Assert.IsType<FunctionDeclaration>(Assert.Single(program.Declarations));
    }

    private static Expression PrintArgument(string body)
    {
        FunctionDeclaration main = ParseMain(body);
        ExpressionStatement statement = Assert.IsType<ExpressionStatement>(Assert.Single(main.Body.Statements));
        CallExpression print = Assert.IsType<CallExpression>(statement.Expression);

        Assert.Equal("print", print.Name);
        return Assert.Single(print.Arguments);
    }
}