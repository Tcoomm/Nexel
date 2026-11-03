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

    [Theory]
    [InlineData("print(2 + 3);", BinaryOperator.Add, 2, 3)]
    [InlineData("print(7 - 4);", BinaryOperator.Subtract, 7, 4)]
    [InlineData("print(3 * 4);", BinaryOperator.Multiply, 3, 4)]
    [InlineData("print(20 / 5);", BinaryOperator.Divide, 20, 5)]
    [InlineData("print(17 % 5);", BinaryOperator.Modulo, 17, 5)]
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
        Assert.Throws<ParserException>(() => Parse("void main() { } void main() { }"));
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
            "int main() { }",
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