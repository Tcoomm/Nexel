using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class StringLiteralTests
{
    public static TheoryData<string, string> ValidStringData =>
        new TheoryData<string, string>
        {
            { "\"\"", string.Empty },
            { "\"Hello\"", "Hello" },
            { "\"Hello world\"", "Hello world" },
            { "\"Hello\\nWorld\"", "Hello\\nWorld" },
            { "\"Column1\\tColumn2\"", "Column1\\tColumn2" },
            { "\"\\\"Nexel\\\"\"", "\\\"Nexel\\\"" },
            { "\"C:\\\\Temp\\\\file.txt\"", "C:\\\\Temp\\\\file.txt" },
            { "\"row\\rnext\"", "row\\rnext" },
            { "\"slash\\\\value\"", "slash\\\\value" },
        };

    [Theory]
    [MemberData(nameof(ValidStringData))]
    public void Tokenize_ValidString_ReturnsStringLiteral(string input, string expectedLexeme)
    {
        Lexer lexer = new Lexer(input);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal(expectedLexeme, tokens[0].Lexeme);
    }

    public static TheoryData<string> UnterminatedStringData =>
        new TheoryData<string>
        {
            "\"Hello",
            "\"Hello\nWorld\"",
            "\"Hello\rWorld\"",
            "\"ends-with-backslash\\",
        };

    [Theory]
    [MemberData(nameof(UnterminatedStringData))]
    public void Tokenize_UnterminatedString_ThrowsLexerException(string input)
    {
        Lexer lexer = new Lexer(input);

        LexerException exception = Assert.Throws<LexerException>(() => lexer.Tokenize());

        Assert.Contains("Unterminated string literal", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Tokenize_UnknownEscape_ThrowsLexerExceptionAtEscapePosition()
    {
        Lexer lexer = new Lexer("\"ab\\q\"");

        LexerException exception = Assert.Throws<LexerException>(() => lexer.Tokenize());

        Assert.Contains("Unknown escape sequence", exception.Message, StringComparison.Ordinal);
        Assert.Equal(new SourcePosition(1, 4), exception.Position);
    }
}
