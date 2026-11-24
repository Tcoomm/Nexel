namespace Nexel.Semantics.UnitTests;

using System.Collections.Generic;

using Nexel.Ast;
using Nexel.Lexer;
using Nexel.Semantics;

public sealed class ControlFlowSemanticTests
{
    [Theory]
    [InlineData("void main() { if (1) { } }")]
    [InlineData("void main() { while (1) { } }")]
    [InlineData("void main() { for (int i = 0; i; i = i + 1) { } }")]
    [InlineData("void main() { print(true + 1); }")]
    [InlineData("void main() { bool x = 1 && true; }")]
    [InlineData("void main() { bool x = 1; }")]
    [InlineData("void main() { print(true); }")]
    [InlineData("int f(int x) { if (x > 0) { return 1; } } void main() { }")]
    [InlineData("void main() { for (int i = 0; i < 1; i = i + 1) { } print(i); }")]
    public void InvalidProgram_ThrowsSemanticException(string source)
    {
        ProgramNode program = Parse(source);
        Assert.Throws<SemanticException>(() => SemanticAnalyzer.Analyze(program));
    }

    private static ProgramNode Parse(string source)
    {
        IReadOnlyList<Token> tokens = new Lexer(source).Tokenize();
        return new global::Nexel.Parser.Parser(tokens).ParseProgram();
    }
}