using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class WhitespaceTests
{
    [Fact]
    public void Tokenize_CodeWithAllWhitespaceKinds_ReturnsSameTokenKinds()
    {
        Lexer compactLexer = new Lexer("int value=10;");
        Lexer spacedLexer = new Lexer(" \tint\r\nvalue \t=\n10\r; \t");

        IReadOnlyList<Token> compactTokens = compactLexer.Tokenize();
        IReadOnlyList<Token> spacedTokens = spacedLexer.Tokenize();

        Assert.Equal(
            compactTokens.Select(token => token.Kind),
            spacedTokens.Select(token => token.Kind));
        Assert.Equal(
            compactTokens.Select(token => token.Lexeme),
            spacedTokens.Select(token => token.Lexeme));
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ReturnsEndOfFile()
    {
        Lexer lexer = new Lexer(" \t\r\n");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Token token = Assert.Single(tokens);
        Assert.Equal(TokenKind.EndOfFile, token.Kind);
        Assert.Equal(new SourcePosition(2, 1), token.Position);
    }
}
