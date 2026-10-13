namespace Nexel.Parser;

using System.Collections.Generic;
using System.Globalization;

using Nexel.Lexer;
using Nexel.Vm;

public sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private readonly List<Instruction> _instructions;
    private int _index;

    public Parser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _instructions = new List<Instruction>();
        _index = 0;
    }

    private Token Current => _tokens[_index];

    public IReadOnlyList<Instruction> ParseProgram()
    {
        Expect(TokenKind.Void);
        ExpectIdentifier("main");
        Expect(TokenKind.LeftParen);
        Expect(TokenKind.RightParen);
        Expect(TokenKind.LeftBrace);

        while (Current.Kind != TokenKind.RightBrace)
        {
            ParseStatement();
        }

        Expect(TokenKind.RightBrace);
        Expect(TokenKind.EndOfFile);

        return _instructions;
    }

    private void ParseStatement()
    {
        if (IsIdentifier(Current, "print"))
        {
            ParsePrintStatement();
            return;
        }

        ParseExpressionStatement();
    }

    private void ParsePrintStatement()
    {
        ExpectIdentifier("print");
        Expect(TokenKind.LeftParen);
        ParseExpression();
        Expect(TokenKind.RightParen);
        Expect(TokenKind.Semicolon);

        Emit(new Instruction(OpCode.Print));
    }

    private void ParseExpressionStatement()
    {
        ParseExpression();
        Expect(TokenKind.Semicolon);

        Emit(new Instruction(OpCode.Pop));
    }

    private void ParseExpression()
    {
        ParseAdditive();
    }

    private void ParseAdditive()
    {
        ParseMultiplicative();

        while (Current.Kind == TokenKind.Plus || Current.Kind == TokenKind.Minus)
        {
            TokenKind op = Current.Kind;
            Advance();

            ParseMultiplicative();

            Emit(new Instruction(op == TokenKind.Plus ? OpCode.Add : OpCode.Sub));
        }
    }

    private void ParseMultiplicative()
    {
        ParseUnary();

        while (Current.Kind == TokenKind.Star
            || Current.Kind == TokenKind.Slash
            || Current.Kind == TokenKind.Percent)
        {
            TokenKind op = Current.Kind;
            Advance();

            ParseUnary();

            OpCode code;

            switch (op)
            {
                case TokenKind.Star:
                    code = OpCode.Mul;
                    break;
                case TokenKind.Slash:
                    code = OpCode.Div;
                    break;
                case TokenKind.Percent:
                    code = OpCode.Mod;
                    break;
                default:
                    throw new ParserException(
                        $"Unexpected operator {op}",
                        Current.Position);
            }

            Emit(new Instruction(code));
        }
    }

    private void ParseUnary()
    {
        if (Current.Kind == TokenKind.Plus)
        {
            Advance();
            ParseUnary();
            return;
        }

        if (Current.Kind == TokenKind.Minus)
        {
            Advance();
            ParseUnary();
            Emit(new Instruction(OpCode.Neg));
            return;
        }

        ParsePrimary();
    }

    private void ParsePrimary()
    {
        if (Current.Kind == TokenKind.IntegerLiteral)
        {
            if (!int.TryParse(
                Current.Lexeme,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int value))
            {
                throw new ParserException(
                    $"Integer literal is out of range: {Current.Lexeme}",
                    Current.Position);
            }

            Advance();
            Emit(new Instruction(OpCode.Push, value));
            return;
        }

        if (Current.Kind == TokenKind.LeftParen)
        {
            Advance();
            ParseExpression();
            Expect(TokenKind.RightParen);
            return;
        }

        if (IsIdentifier(Current, "readInt"))
        {
            Advance();
            Expect(TokenKind.LeftParen);
            Expect(TokenKind.RightParen);
            Emit(new Instruction(OpCode.ReadInt));
            return;
        }

        throw new ParserException(
            $"Unexpected token {Current.Kind} '{Current.Lexeme}', expected expression",
            Current.Position);
    }

    private void Advance()
    {
        if (_index < _tokens.Count - 1)
        {
            _index++;
        }
    }

    private void Expect(TokenKind kind)
    {
        if (Current.Kind != kind)
        {
            throw new ParserException(
                $"Expected {kind}, got {Current.Kind} '{Current.Lexeme}'",
                Current.Position);
        }

        Advance();
    }

    private void ExpectIdentifier(string lexeme)
    {
        if (!IsIdentifier(Current, lexeme))
        {
            throw new ParserException(
                $"Expected identifier '{lexeme}', got {Current.Kind} '{Current.Lexeme}'",
                Current.Position);
        }

        Advance();
    }

    private static bool IsIdentifier(Token token, string lexeme)
    {
        return token.Kind == TokenKind.Identifier
            && token.Lexeme == lexeme;
    }

    private void Emit(Instruction instruction)
    {
        _instructions.Add(instruction);
    }
}