namespace Nexel.Parser.UnitTests;

using System.Collections.Generic;

using Nexel.Lexer;
using Nexel.Parser;
using Nexel.Vm;

using Xunit;

public sealed class ParserTests
{
    [Fact]
    public void ParseEmptyMain()
    {
        AssertProgram(string.Empty);
    }

    [Fact]
    public void ParsePrintNumber()
    {
        AssertProgram(
            "print(5);",
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintAdd()
    {
        AssertProgram(
            "print(2 + 3);",
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintSub()
    {
        AssertProgram(
            "print(7 - 4);",
            new Instruction(OpCode.Push, 7),
            new Instruction(OpCode.Push, 4),
            new Instruction(OpCode.Sub),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintMul()
    {
        AssertProgram(
            "print(3 * 4);",
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Push, 4),
            new Instruction(OpCode.Mul),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintDiv()
    {
        AssertProgram(
            "print(20 / 5);",
            new Instruction(OpCode.Push, 20),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Div),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintMod()
    {
        AssertProgram(
            "print(17 % 5);",
            new Instruction(OpCode.Push, 17),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Mod),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintUnaryMinus()
    {
        AssertProgram(
            "print(-5);",
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Neg),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintUnaryPlusIsNoOp()
    {
        AssertProgram(
            "print(+5);",
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintReadInt()
    {
        AssertProgram(
            "print(readInt());",
            new Instruction(OpCode.ReadInt),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintReadIntPlusOne()
    {
        AssertProgram(
            "print(readInt() + 1);",
            new Instruction(OpCode.ReadInt),
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintPrecedenceMulBeforeAdd()
    {
        AssertProgram(
            "print(10 + 2 * 5);",
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Mul),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParsePrintParenthesesOverridePrecedence()
    {
        AssertProgram(
            "print((10 + 2) * 5);",
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Mul),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParseLeftAssociativityOfSubtraction()
    {
        AssertProgram(
            "print(10 - 3 - 2);",
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Sub),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Sub),
            new Instruction(OpCode.Print));
    }

    [Fact]
    public void ParseExpressionStatementEmitsPop()
    {
        AssertProgram(
            "7 + 3;",
            new Instruction(OpCode.Push, 7),
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Pop));
    }

    [Fact]
    public void ParseMultipleStatements()
    {
        AssertProgram(
            "print(1); print(2);",
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Print));
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
    public void ParseUnexpectedEofThrows()
    {
        Assert.Throws<ParserException>(
            () => Parse("void main() { print("));
    }

    [Fact]
    public void ParseRequiresEndOfFile()
    {
        Assert.Throws<ParserException>(
            () => Parse("void main() { } void main() { }"));
    }

    public static TheoryData<string> InvalidPrograms()
    {
        return new TheoryData<string>
        {
            "void main() { print(); }",
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

    private static IReadOnlyList<Instruction> Parse(string source)
    {
        Lexer lexer = new Lexer(source);
        IReadOnlyList<Token> tokens = lexer.Tokenize();
        Parser parser = new Parser(tokens);
        return parser.ParseProgram();
    }

    private static void AssertProgram(string body, params Instruction[] expected)
    {
        string source = "void main() { " + body + " }";
        IReadOnlyList<Instruction> actual = Parse(source);

        Assert.Equal(expected.Length, actual.Count);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Code, actual[i].Code);
            Assert.Equal(expected[i].Operand, actual[i].Operand);
        }
    }
}
