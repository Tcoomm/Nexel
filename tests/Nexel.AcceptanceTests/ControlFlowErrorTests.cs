using Nexel.Semantics;

namespace Nexel.AcceptanceTests;

public sealed class ControlFlowErrorTests
{
    public static TheoryData<string> InvalidProgramData =>
        new TheoryData<string>
        {
            "void main() { if (1) { } }",
            "void main() { while (1) { } }",
            "void main() { for (int i = 0; i; i = i + 1) { } }",
            "void main() { print(true + 1); }",
            "void main() { bool x = 1 && true; }",
            "void main() { bool x = 1; }",
            "void main() { print(true); }",
            "int f(int x) { if (x > 0) { return 1; } } void main() { }",
            "void main() { for (int i = 0; i < 1; i = i + 1) { } print(i); }",
        };

    [Theory]
    [MemberData(nameof(InvalidProgramData))]
    public void InvalidProgram_ThrowsSemanticException(string source)
    {
        Assert.Throws<SemanticException>(() => InterpreterTestRunner.Run(source));
    }
}