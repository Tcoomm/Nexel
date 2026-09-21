using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class KeywordTests
{
    public static TheoryData<string, TokenKind> KeywordData =>
        new TheoryData<string, TokenKind>
        {
            { "int", TokenKind.Int },
            { "string", TokenKind.String },
            { "bool", TokenKind.Bool },
            { "void", TokenKind.Void },
            { "if", TokenKind.If },
            { "else", TokenKind.Else },
            { "while", TokenKind.While },
            { "return", TokenKind.Return },
            { "struct", TokenKind.Struct },
            { "true", TokenKind.True },
            { "false", TokenKind.False },
        };

    [Theory]
    [MemberData(nameof(KeywordData))]
    public void Tokenize_Keyword_ReturnsExpectedKind(string input, TokenKind expectedKind)
    {
        Lexer lexer = new Lexer(input);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedKind, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Lexeme);
        Assert.Equal(TokenKind.EndOfFile, tokens[1].Kind);
    }
}
