using System.Text;

namespace Nexel.Lexer;

public sealed class Lexer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        ["int"] = TokenKind.Int,
        ["string"] = TokenKind.String,
        ["bool"] = TokenKind.Bool,
        ["void"] = TokenKind.Void,
        ["if"] = TokenKind.If,
        ["else"] = TokenKind.Else,
        ["while"] = TokenKind.While,
        ["return"] = TokenKind.Return,
        ["struct"] = TokenKind.Struct,
        ["true"] = TokenKind.True,
        ["false"] = TokenKind.False,
    };

    private readonly string _source;
    private int _index;
    private int _line;
    private int _column;

    public Lexer(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _index = 0;
        _line = 1;
        _column = 1;
    }

    private bool IsAtEnd => _index >= _source.Length;

    private char Current => _source[_index];

    private SourcePosition CurrentPosition => new SourcePosition(_line, _column);

    public IReadOnlyList<Token> Tokenize()
    {
        List<Token> tokens = new List<Token>();

        while (true)
        {
            SkipTrivia();

            if (IsAtEnd)
            {
                tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, CurrentPosition));
                return tokens;
            }

            tokens.Add(NextToken());
        }
    }

    private char Peek(int offset = 1)
    {
        int pos = _index + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private char Advance()
    {
        char c = _source[_index];
        _index++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd || Current != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    private void SkipTrivia()
    {
        while (!IsAtEnd)
        {
            char c = Current;

            if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
            {
                Advance();
                continue;
            }

            if (c == '/' && Peek() == '/')
            {
                while (!IsAtEnd && Current != '\n')
                {
                    Advance();
                }

                continue;
            }

            if (c == '/' && Peek() == '*')
            {
                SourcePosition start = CurrentPosition;
                Advance();
                Advance();

                bool closed = false;

                while (!IsAtEnd)
                {
                    if (Current == '*' && Peek() == '/')
                    {
                        Advance();
                        Advance();
                        closed = true;
                        break;
                    }

                    Advance();
                }

                if (!closed)
                {
                    throw new LexerException("Unterminated multi-line comment", start);
                }

                continue;
            }

            break;
        }
    }

    private Token NextToken()
    {
        SourcePosition start = CurrentPosition;
        char c = Current;

        if (IsIdentifierStart(c))
        {
            return ReadIdentifierOrKeyword(start);
        }

        if (c >= '0' && c <= '9')
        {
            return ReadIntegerLiteral(start);
        }

        if (c == '"')
        {
            return ReadStringLiteral(start);
        }

        return ReadOperatorOrSeparator(start);
    }

    private static bool IsIdentifierStart(char c) =>
        (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_';

    private static bool IsIdentifierPart(char c) =>
        (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
        (c >= '0' && c <= '9') || c == '_';

    private Token ReadIdentifierOrKeyword(SourcePosition start)
    {
        int begin = _index;

        while (!IsAtEnd && IsIdentifierPart(Current))
        {
            Advance();
        }

        string text = _source.Substring(begin, _index - begin);
        TokenKind kind = Keywords.TryGetValue(text, out TokenKind kw) ? kw : TokenKind.Identifier;
        return new Token(kind, text, start);
    }

    private Token ReadIntegerLiteral(SourcePosition start)
    {
        int begin = _index;

        while (!IsAtEnd && Current >= '0' && Current <= '9')
        {
            Advance();
        }

        string text = _source.Substring(begin, _index - begin);
        return new Token(TokenKind.IntegerLiteral, text, start);
    }

    private Token ReadStringLiteral(SourcePosition start)
    {
        Advance();

        int begin = _index;
        StringBuilder processed = new StringBuilder();

        while (true)
        {
            if (IsAtEnd)
            {
                throw new LexerException("Unterminated string literal", start);
            }

            char c = Current;

            if (c == '\n' || c == '\r')
            {
                throw new LexerException("Unterminated string literal", start);
            }

            if (c == '"')
            {
                Advance();
                string raw = _source.Substring(begin, _index - begin - 1);
                return new Token(TokenKind.StringLiteral, raw, start);
            }

            if (c == '\\')
            {
                SourcePosition escPos = CurrentPosition;
                Advance();

                if (IsAtEnd)
                {
                    throw new LexerException("Unterminated string literal", start);
                }

                char esc = Current;

                switch (esc)
                {
                    case '"':
                        processed.Append('"');
                        break;

                    case '\\':
                        processed.Append('\\');
                        break;

                    case 'n':
                        processed.Append('\n');
                        break;

                    case 'r':
                        processed.Append('\r');
                        break;

                    case 't':
                        processed.Append('\t');
                        break;

                    default:
                        throw new LexerException($"Unknown escape sequence '\\{esc}'", escPos);
                }

                Advance();
                continue;
            }

            processed.Append(c);
            Advance();
        }
    }

    private Token ReadOperatorOrSeparator(SourcePosition start)
    {
        char c = Current;

        switch (c)
        {
            case '+':
                Advance();
                return new Token(TokenKind.Plus, "+", start);

            case '-':
                Advance();
                return new Token(TokenKind.Minus, "-", start);

            case '*':
                Advance();
                return new Token(TokenKind.Star, "*", start);

            case '/':
                Advance();
                return new Token(TokenKind.Slash, "/", start);

            case '%':
                Advance();
                return new Token(TokenKind.Percent, "%", start);

            case '=':
                Advance();
                if (Match('='))
                {
                    return new Token(TokenKind.Equal, "==", start);
                }

                return new Token(TokenKind.Assign, "=", start);

            case '!':
                Advance();
                if (Match('='))
                {
                    return new Token(TokenKind.NotEqual, "!=", start);
                }

                return new Token(TokenKind.Not, "!", start);

            case '<':
                Advance();
                if (Match('='))
                {
                    return new Token(TokenKind.LessOrEqual, "<=", start);
                }

                return new Token(TokenKind.Less, "<", start);

            case '>':
                Advance();
                if (Match('='))
                {
                    return new Token(TokenKind.GreaterOrEqual, ">=", start);
                }

                return new Token(TokenKind.Greater, ">", start);

            case '&':
                Advance();
                if (Match('&'))
                {
                    return new Token(TokenKind.And, "&&", start);
                }

                throw new LexerException("Unexpected character '&'", start);

            case '|':
                Advance();
                if (Match('|'))
                {
                    return new Token(TokenKind.Or, "||", start);
                }

                throw new LexerException("Unexpected character '|'", start);

            case '(':
                Advance();
                return new Token(TokenKind.LeftParen, "(", start);

            case ')':
                Advance();
                return new Token(TokenKind.RightParen, ")", start);

            case '{':
                Advance();
                return new Token(TokenKind.LeftBrace, "{", start);

            case '}':
                Advance();
                return new Token(TokenKind.RightBrace, "}", start);

            case '[':
                Advance();
                return new Token(TokenKind.LeftBracket, "[", start);

            case ']':
                Advance();
                return new Token(TokenKind.RightBracket, "]", start);

            case ';':
                Advance();
                return new Token(TokenKind.Semicolon, ";", start);

            case ',':
                Advance();
                return new Token(TokenKind.Comma, ",", start);

            case '.':
                Advance();
                return new Token(TokenKind.Dot, ".", start);
        }

        throw new LexerException($"Unexpected character '{c}'", start);
    }
}