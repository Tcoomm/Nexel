namespace Nexel.Parser;

using System.Collections.Generic;
using System.Globalization;

using Nexel.Ast;
using Nexel.Lexer;

public sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _index;

    public Parser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _index = 0;
    }

    private Token Current => _tokens[_index];

    public ProgramNode ParseProgram()
    {
        AstPosition programPosition = Position(Current);
        Expect(TokenKind.Void);

        Token mainToken = Current;
        ExpectIdentifier("main");
        Expect(TokenKind.LeftParen);
        Expect(TokenKind.RightParen);
        BlockStatement body = ParseBlock();
        Expect(TokenKind.EndOfFile);

        FunctionDeclaration main = new FunctionDeclaration(
            Position(mainToken),
            LangType.Void,
            "main",
            Array.Empty<ParameterNode>(),
            body);

        return new ProgramNode(programPosition, new Declaration[] { main });
    }

    private BlockStatement ParseBlock()
    {
        Token start = Current;
        Expect(TokenKind.LeftBrace);

        List<Statement> statements = new List<Statement>();

        while (Current.Kind != TokenKind.RightBrace && Current.Kind != TokenKind.EndOfFile)
        {
            statements.Add(ParseStatement());
        }

        Expect(TokenKind.RightBrace);
        return new BlockStatement(Position(start), statements);
    }

    private Statement ParseStatement()
    {
        AstPosition position = Position(Current);
        Expression expression = ParseExpression();
        Expect(TokenKind.Semicolon);
        return new ExpressionStatement(position, expression);
    }

    private Expression ParseExpression()
    {
        return ParseAdditive();
    }

    private Expression ParseAdditive()
    {
        Expression left = ParseMultiplicative();

        while (Current.Kind == TokenKind.Plus || Current.Kind == TokenKind.Minus)
        {
            Token op = Current;
            Advance();
            Expression right = ParseMultiplicative();

            BinaryOperator kind = op.Kind == TokenKind.Plus
                ? BinaryOperator.Add
                : BinaryOperator.Subtract;

            left = new BinaryExpression(Position(op), kind, left, right);
        }

        return left;
    }

    private Expression ParseMultiplicative()
    {
        Expression left = ParseUnary();

        while (Current.Kind == TokenKind.Star
            || Current.Kind == TokenKind.Slash
            || Current.Kind == TokenKind.Percent)
        {
            Token op = Current;
            Advance();
            Expression right = ParseUnary();

            BinaryOperator kind = op.Kind switch
            {
                TokenKind.Star => BinaryOperator.Multiply,
                TokenKind.Slash => BinaryOperator.Divide,
                TokenKind.Percent => BinaryOperator.Modulo,
                _ => throw new ParserException($"Unexpected operator {op.Kind}", op.Position),
            };

            left = new BinaryExpression(Position(op), kind, left, right);
        }

        return left;
    }

    private Expression ParseUnary()
    {
        if (Current.Kind == TokenKind.Plus || Current.Kind == TokenKind.Minus)
        {
            Token op = Current;
            Advance();
            Expression operand = ParseUnary();

            UnaryOperator kind = op.Kind == TokenKind.Plus
                ? UnaryOperator.Plus
                : UnaryOperator.Negate;

            return new UnaryExpression(Position(op), kind, operand);
        }

        return ParsePrimary();
    }

    private Expression ParsePrimary()
    {
        Token token = Current;

        if (Current.Kind == TokenKind.IntegerLiteral)
        {
            if (!int.TryParse(
                token.Lexeme,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int value))
            {
                throw new ParserException(
                    $"Integer literal is out of range: {token.Lexeme}",
                    token.Position);
            }

            Advance();
            return new IntegerLiteralExpression(Position(token), value);
        }

        if (Current.Kind == TokenKind.LeftParen)
        {
            Advance();
            Expression expression = ParseExpression();
            Expect(TokenKind.RightParen);
            return expression;
        }

        if (Current.Kind == TokenKind.Identifier)
        {
            Advance();

            if (Current.Kind != TokenKind.LeftParen)
            {
                return new VariableExpression(Position(token), token.Lexeme);
            }

            Advance();
            List<Expression> arguments = new List<Expression>();

            if (Current.Kind != TokenKind.RightParen)
            {
                arguments.Add(ParseExpression());

                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    arguments.Add(ParseExpression());
                }
            }

            Expect(TokenKind.RightParen);
            return new CallExpression(Position(token), token.Lexeme, arguments);
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
        if (Current.Kind != TokenKind.Identifier || Current.Lexeme != lexeme)
        {
            throw new ParserException(
                $"Expected identifier '{lexeme}', got {Current.Kind} '{Current.Lexeme}'",
                Current.Position);
        }

        Advance();
    }

    private static AstPosition Position(Token token)
    {
        return new AstPosition(token.Position.Line, token.Position.Column);
    }
}