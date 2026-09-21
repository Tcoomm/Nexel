using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class SeparatorTests
{
    public static TheoryData<string, TokenKind> SeparatorData =>
        new TheoryData<string, TokenKind>
        {
            { "(", TokenKind.LeftParen },
            { ")", TokenKind.RightParen },
            { "{", TokenKind.LeftBrace },
            { "}", TokenKind.RightBrace },
            { "[", TokenKind.LeftBracket },
            { "]", TokenKind.RightBracket },
            { ";", TokenKind.Semicolon },
            { ",", TokenKind.Comma },
            { ".", TokenKind.Dot },
        };

    [Theory]
    [MemberData(nameof(SeparatorData))]
    public void Tokenize_Separator_ReturnsExpectedKind(string input, TokenKind expectedKind)
    {
        Lexer lexer = new Lexer(input);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedKind, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Lexeme);
    }
}