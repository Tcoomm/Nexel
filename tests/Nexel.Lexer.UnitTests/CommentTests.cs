using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class CommentTests
{
    public static TheoryData<string> CommentedCodeData =>
        new TheoryData<string>
        {
            "// comment\nint x = 10;",
            "int x = 10; // comment",
            "/* comment */ int x = 10;",
            "/*\n multi\n line\n*/\nint x = 10;",
        };

    [Theory]
    [MemberData(nameof(CommentedCodeData))]
    public void Tokenize_Comments_AreIgnored(string input)
    {
        Lexer lexer = new Lexer(input);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        TokenKind[] expectedKinds =
        {
            TokenKind.Int,
            TokenKind.Identifier,
            TokenKind.Assign,
            TokenKind.IntegerLiteral,
            TokenKind.Semicolon,
            TokenKind.EndOfFile,
        };

        Assert.Equal(expectedKinds, tokens.Select(token => token.Kind));
    }

    [Fact]
    public void Tokenize_UnterminatedMultiLineComment_ThrowsLexerException()
    {
        Lexer lexer = new Lexer("/* comment");

        LexerException exception = Assert.Throws<LexerException>(() => lexer.Tokenize());

        Assert.Contains("Unterminated multi-line comment", exception.Message, StringComparison.Ordinal);
        Assert.Equal(new SourcePosition(1, 1), exception.Position);
    }
}
