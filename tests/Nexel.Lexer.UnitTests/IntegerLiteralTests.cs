using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class IntegerLiteralTests
{
    public static TheoryData<string> IntegerData =>
        new TheoryData<string>
        {
            "0",
            "1",
            "42",
            "100",
            "100500",
        };

    [Theory]
    [MemberData(nameof(IntegerData))]
    public void Tokenize_Integer_ReturnsIntegerLiteral(string input)
    {
        Lexer lexer = new Lexer(input);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.IntegerLiteral, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_NegativeInteger_ReturnsMinusAndIntegerLiteral()
    {
        Lexer lexer = new Lexer("-42");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenKind.Minus, tokens[0].Kind);
        Assert.Equal("-", tokens[0].Lexeme);
        Assert.Equal(TokenKind.IntegerLiteral, tokens[1].Kind);
        Assert.Equal("42", tokens[1].Lexeme);
        Assert.Equal(TokenKind.EndOfFile, tokens[2].Kind);
    }
}
