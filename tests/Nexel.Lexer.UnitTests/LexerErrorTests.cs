using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class LexerErrorTests
{
    public static TheoryData<string, string> InvalidCharacterData =>
        new TheoryData<string, string>
        {
            { "&", "Unexpected character '&'" },
            { "|", "Unexpected character '|'" },
            { "@", "Unexpected character '@'" },
        };

    [Theory]
    [MemberData(nameof(InvalidCharacterData))]
    public void Tokenize_InvalidCharacter_ThrowsLexerException(string input, string expectedMessage)
    {
        Lexer lexer = new Lexer(input);

        LexerException exception = Assert.Throws<LexerException>(() => lexer.Tokenize());

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
        Assert.Equal(new SourcePosition(1, 1), exception.Position);
    }

    [Fact]
    public void Constructor_NullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Lexer(null!));
    }
}
