using Nexel.Lexer;
using Nexel.Parser;
using Nexel.Vm;

namespace Nexel.AcceptanceTests;

public sealed class InterpreterTests
{
    [Theory]
    [InlineData("void main() { }", "", "")]
    [InlineData("void main() { print(10 + 2 * 5); }", "", "20\n")]
    [InlineData("void main() { print((10 + 2) * 5); }", "", "60\n")]
    [InlineData("void main() { print(10 - 3 - 2); }", "", "5\n")]
    [InlineData("void main() { print(-5 + 8 % 3); }", "", "-3\n")]
    [InlineData("void main() { print(+7); }", "", "7\n")]
    [InlineData("void main() { print(20 / 4); }", "", "5\n")]
    [InlineData("void main() { 2 + 3; print(6); }", "", "6\n")]
    [InlineData("void main() { print(readInt() + 1); }", "7\n", "8\n")]
    [InlineData("void main() { print(1); print(2); }", "", "1\n2\n")]
    public void ExecuteProgram(string source, string input, string expected)
    {
        Assert.Equal(expected, Run(source, input));
    }

    [Fact]
    public void InvalidSyntaxThrowsParserException()
    {
        Assert.Throws<ParserException>(
            () => Run("void main() { print(1 + ); }", ""));
    }

    [Fact]
    public void DivisionByZeroThrowsVmException()
    {
        Assert.Throws<VmException>(
            () => Run("void main() { print(10 / 0); }", ""));
    }

    [Fact]
    public void InvalidInputThrowsVmException()
    {
        Assert.Throws<VmException>(
            () => Run("void main() { print(readInt()); }", "abc\n"));
    }

    [Fact]
    public void InvalidCharacterThrowsLexerException()
    {
        Assert.Throws<LexerException>(
            () => Run("void main() { print(@); }", ""));
    }

    private static string Run(string source, string input)
    {
        IReadOnlyList<Token> tokens = new global::Nexel.Lexer.Lexer(source).Tokenize();
        IReadOnlyList<Instruction> instructions =
            new global::Nexel.Parser.Parser(tokens).ParseProgram();

        using StringReader reader = new StringReader(input);
        using StringWriter writer = new StringWriter();

        new VirtualMachine().Execute(instructions, reader, writer);

        return writer.ToString().Replace("\r\n", "\n");
    }
}