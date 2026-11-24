namespace Nexel.Lexer;

public enum TokenKind
{
    Identifier,
    IntegerLiteral,
    DoubleLiteral,
    StringLiteral,

    Int,
    Double,
    String,
    Bool,
    Void,
    If,
    Else,
    While,
    For,
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
    PlusAssign,
    MinusAssign,
    StarAssign,
    SlashAssign,

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