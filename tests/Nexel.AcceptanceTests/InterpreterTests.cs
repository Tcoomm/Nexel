using Nexel.Execution;
using Nexel.Lexer;
using Nexel.Parser;
using Nexel.Vm;

namespace Nexel.AcceptanceTests;

public sealed class InterpreterTests
{
    public static TheoryData<string, string, string> ExecuteProgramCases()
    {
        return new TheoryData<string, string, string>
        {
            { "void main() { }", "", "" },
            { "void main() { print(10 + 2 * 5); }", "", "20\n" },
            { "void main() { print((10 + 2) * 5); }", "", "60\n" },
            { "void main() { print(10 - 3 - 2); }", "", "5\n" },
            { "void main() { print(-5 + 8 % 3); }", "", "-3\n" },
            { "void main() { print(+7); }", "", "7\n" },
            { "void main() { print(20 / 4); }", "", "5\n" },
            { "void main() { 2 + 3; print(6); }", "", "6\n" },
            { "void main() { print(readInt() + 1); }", "7\n", "8\n" },
            { "void main() { print(1); print(2); }", "", "1\n2\n" },
        };
    }

    [Theory]
    [MemberData(nameof(ExecuteProgramCases))]
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
        using StringReader reader = new StringReader(input);
        using StringWriter writer = new StringWriter();

        new Interpreter().Execute(source, reader, writer);

        return writer.ToString().Replace("\r\n", "\n");
    }
}