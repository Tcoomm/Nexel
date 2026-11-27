namespace Nexel.Parser.UnitTests;

using System.Collections.Generic;

using Nexel.Ast;
using Nexel.Lexer;
using Nexel.Parser;

using Xunit;

public sealed class ControlFlowParserTests
{
    public static TheoryData<string, LangType> BoolTypePositions =>
        new TheoryData<string, LangType>
        {
            { "bool f() { return true; } void main() { }", LangType.Bool },
            { "void f(bool flag) { } void main() { }", LangType.Bool },
            { "void main() { bool x = true; }", LangType.Bool },
            { "bool g = false; void main() { }", LangType.Bool },
        };

    public static TheoryData<string> InvalidControlFlowSyntax =>
        new TheoryData<string>
        {
            "void main() { if true { } }",
            "void main() { if (true { } }",
            "void main() { while true { } }",
            "void main() { for (int i = 0; i < 3) { } }",
            "void main() { for (int i = 0; i < 3; i = i + 1) print(i); }",
            "void main() { if (true) print(1); }",
        };

    [Theory]
    [MemberData(nameof(BoolTypePositions))]
    public void ParseBoolType_IsAccepted(string source, LangType expected)
    {
        ProgramNode program = Parse(source);
        Assert.NotEmpty(program.Declarations);
        _ = expected;
    }

    [Fact]
    public void ParseBooleanLiterals_CreateBooleanLiteralExpression()
    {
        FunctionDeclaration main = ParseMain("bool a = true; bool b = false;");
        VariableDeclarationStatement first =
            Assert.IsType<VariableDeclarationStatement>(main.Body.Statements[0]);
        VariableDeclarationStatement second =
            Assert.IsType<VariableDeclarationStatement>(main.Body.Statements[1]);

        Assert.True(Assert.IsType<BooleanLiteralExpression>(first.Declaration.Initializer).Value);
        Assert.False(Assert.IsType<BooleanLiteralExpression>(second.Declaration.Initializer).Value);
    }

    [Fact]
    public void ParseIfWithoutElse_CreatesIfStatement()
    {
        FunctionDeclaration main = ParseMain("if (true) { print(1); }");
        IfStatement statement = Assert.IsType<IfStatement>(Assert.Single(main.Body.Statements));

        Assert.IsType<BooleanLiteralExpression>(statement.Condition);
        Assert.Single(statement.ThenBlock.Statements);
        Assert.Null(statement.ElseBlock);
    }

    [Fact]
    public void ParseIfWithElse_CreatesBothBlocks()
    {
        FunctionDeclaration main = ParseMain("if (true) { print(1); } else { print(0); }");
        IfStatement statement = Assert.IsType<IfStatement>(Assert.Single(main.Body.Statements));

        Assert.NotNull(statement.ElseBlock);
        Assert.Single(statement.ThenBlock.Statements);
        Assert.Single(statement.ElseBlock!.Statements);
    }

    [Fact]
    public void ParseWhile_CreatesWhileStatement()
    {
        FunctionDeclaration main = ParseMain("int i = 0; while (i < 3) { i = i + 1; }");
        WhileStatement statement = Assert.IsType<WhileStatement>(main.Body.Statements[1]);

        Assert.IsType<BinaryExpression>(statement.Condition);
        Assert.Single(statement.Body.Statements);
    }

    [Fact]
    public void ParseFor_CreatesForWithAllThreeParts()
    {
        FunctionDeclaration main = ParseMain("for (int i = 0; i < 3; i = i + 1) { print(i); }");
        ForStatement statement = Assert.IsType<ForStatement>(Assert.Single(main.Body.Statements));

        Assert.IsType<VariableDeclarationStatement>(statement.Initializer);
        Assert.IsType<BinaryExpression>(statement.Condition);
        Assert.Equal("i", statement.Step.Name);
        Assert.Single(statement.Body.Statements);
    }

    [Fact]
    public void ParseIfWithNonBoolCondition_IsSyntacticallyValid()
    {
        FunctionDeclaration main = ParseMain("if (1) { }");
        IfStatement statement = Assert.IsType<IfStatement>(Assert.Single(main.Body.Statements));

        Assert.IsType<IntegerLiteralExpression>(statement.Condition);
    }

    [Fact]
    public void ParseNotOperator_CreatesUnaryNot()
    {
        BinaryExpression and = Assert.IsType<BinaryExpression>(PrintArgument("print(!true && false);"));
        UnaryExpression not = Assert.IsType<UnaryExpression>(and.Left);

        Assert.Equal(UnaryOperator.Not, not.Operator);
        Assert.True(Assert.IsType<BooleanLiteralExpression>(not.Operand).Value);
    }

    [Theory]
    [MemberData(nameof(InvalidControlFlowSyntax))]
    public void ParseInvalidControlFlow_ThrowsParserException(string source)
    {
        Assert.Throws<ParserException>(() => Parse(source));
    }

    [Fact]
    public void ParseLogicalPrecedence_AndBeforeOr()
    {
        BinaryExpression or = Assert.IsType<BinaryExpression>(
            PrintArgument("print(true || false && true);"));

        Assert.Equal(BinaryOperator.Or, or.Operator);
        Assert.IsType<BooleanLiteralExpression>(or.Left);
        Assert.IsType<BinaryExpression>(or.Right);
        Assert.Equal(BinaryOperator.And, Assert.IsType<BinaryExpression>(or.Right).Operator);
    }

    [Fact]
    public void ParseComparisonPrecedence_RelationalBeforeEquality()
    {
        BinaryExpression equal = Assert.IsType<BinaryExpression>(
            PrintArgument("print(1 < 2 == true);"));

        Assert.Equal(BinaryOperator.Equal, equal.Operator);
        Assert.IsType<BinaryExpression>(equal.Left);
        Assert.Equal(BinaryOperator.Less, Assert.IsType<BinaryExpression>(equal.Left).Operator);
    }

    [Fact]
    public void ParseArithmeticBeforeComparison()
    {
        BinaryExpression less = Assert.IsType<BinaryExpression>(
            PrintArgument("print(1 + 2 < 3 * 4);"));

        Assert.Equal(BinaryOperator.Less, less.Operator);
        Assert.Equal(BinaryOperator.Add, Assert.IsType<BinaryExpression>(less.Left).Operator);
        Assert.Equal(BinaryOperator.Multiply, Assert.IsType<BinaryExpression>(less.Right).Operator);
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
        return Assert.Single(print.Arguments);
    }
}