namespace Nexel.Lexer;

public enum TokenKind
{
    Identifier,
    IntegerLiteral,
    StringLiteral,

    Int,
    String,
    Bool,
    Void,
    If,
    Else,
    While,
    Return,
    Struct,
    True,
    False,

    Plus,
    Minus,
    Star,
    Slash,
    Percent,

    Assign,

    Equal,
    NotEqual,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,

    And,
    Or,
    Not,

    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Semicolon,
    Comma,
    Dot,

    EndOfFile,
}