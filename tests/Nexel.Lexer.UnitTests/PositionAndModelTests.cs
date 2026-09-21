using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class PositionAndModelTests
{
    [Fact]
    public void Tokenize_MultiLineInput_TracksTokenPositions()
    {
        Lexer lexer = new Lexer("int x;\n  string name;");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(new SourcePosition(1, 1), tokens[0].Position);
        Assert.Equal(new SourcePosition(1, 5), tokens[1].Position);
        Assert.Equal(new SourcePosition(1, 6), tokens[2].Position);
        Assert.Equal(new SourcePosition(2, 3), tokens[3].Position);
        Assert.Equal(new SourcePosition(2, 10), tokens[4].Position);
        Assert.Equal(new SourcePosition(2, 14), tokens[5].Position);
    }

    [Fact]
    public void Token_ToString_ContainsKindLexemeAndPosition()
    {
        Token token = new Token(TokenKind.Identifier, "name", new SourcePosition(3, 7));

        string text = token.ToString();

        Assert.Equal("Identifier('name') at (3, 7)", text);
    }

    [Fact]
    public void LexerException_MessageConstructor_PreservesMessage()
    {
        LexerException exception = new LexerException("message");

        Assert.Equal("message", exception.Message);
    }

    [Fact]
    public void LexerException_InnerExceptionConstructor_PreservesInnerException()
    {
        InvalidOperationException inner = new InvalidOperationException("inner");
        LexerException exception = new LexerException("message", inner);

        Assert.Equal("message", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void LexerException_DefaultConstructor_CreatesException()
    {
        LexerException exception = new LexerException();

        Assert.NotNull(exception);
    }
}
