using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class OperatorTests
{
    public static TheoryData<string, TokenKind> OperatorData =>
        new TheoryData<string, TokenKind>
        {
            { "+", TokenKind.Plus },
            { "-", TokenKind.Minus },
            { "*", TokenKind.Star },
            { "/", TokenKind.Slash },
            { "%", TokenKind.Percent },
            { "=", TokenKind.Assign },
            { "==", TokenKind.Equal },
            { "!=", TokenKind.NotEqual },
            { "<", TokenKind.Less },
            { "<=", TokenKind.LessOrEqual },
            { ">", TokenKind.Greater },
            { ">=", TokenKind.GreaterOrEqual },
            { "&&", TokenKind.And },
            { "||", TokenKind.Or },
            { "!", TokenKind.Not },
        };

    [Theory]
    [MemberData(nameof(OperatorData))]
    public void Tokenize_Operator_ReturnsExpectedKind(string input, TokenKind expectedKind)
    {
        Lexer lexer = new Lexer(input);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedKind, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_AdjacentLongAndShortOperators_UsesLongestMatch()
    {
        Lexer lexer = new Lexer("= == < <= > >= ! !=");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        TokenKind[] expectedKinds =
        {
            TokenKind.Assign,
            TokenKind.Equal,
            TokenKind.Less,
            TokenKind.LessOrEqual,
            TokenKind.Greater,
            TokenKind.GreaterOrEqual,
            TokenKind.Not,
            TokenKind.NotEqual,
            TokenKind.EndOfFile,
        };

        Assert.Equal(expectedKinds, tokens.Select(token => token.Kind));
    }
}