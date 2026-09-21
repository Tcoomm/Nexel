using Nexel.Lexer;

namespace Nexel.Lexer.UnitTests;

public sealed class IdentifierTests
{
    public static TheoryData<string> IdentifierData =>
        new TheoryData<string>
        {
            "x",
            "value",
            "Value",
            "_value",
            "value2",
            "some_long_name",
            "While",
            "whileCount",
        };

    [Theory]
    [MemberData(nameof(IdentifierData))]
    public void Tokenize_ValidIdentifier_ReturnsIdentifier(string input)
    {
        Lexer lexer = new Lexer(input);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Lexeme);
        Assert.Equal(TokenKind.EndOfFile, tokens[1].Kind);
    }
}
