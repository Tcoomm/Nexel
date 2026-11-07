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

    private TokenKind NextKind => _index + 1 < _tokens.Count
        ? _tokens[_index + 1].Kind
        : TokenKind.EndOfFile;

    public ProgramNode ParseProgram()
    {
        AstPosition programPosition = Position(Current);
        List<Declaration> declarations = new List<Declaration>();

        while (Current.Kind != TokenKind.EndOfFile)
        {
            declarations.Add(ParseExternalDeclaration());
        }

        Expect(TokenKind.EndOfFile);
        return new ProgramNode(programPosition, declarations);
    }

    private Declaration ParseExternalDeclaration()
    {
        LangType type = ParseFunctionReturnType();
        Token name = ExpectIdentifier();

        if (Current.Kind == TokenKind.LeftParen)
        {
            return ParseFunctionDeclaration(type, name);
        }

        if (type == LangType.Int && Current.Kind == TokenKind.Assign)
        {
            Advance();
            Expression initializer = ParseGlobalInitializer();
            Expect(TokenKind.Semicolon);
            return new VariableDeclaration(Position(name), LangType.Int, name.Lexeme, initializer);
        }

        throw new ParserException(
            $"Expected function parameters or global variable initializer, got {Current.Kind}",
            Current.Position);
    }

    private LangType ParseFunctionReturnType()
    {
        if (Current.Kind == TokenKind.Int)
        {
            Advance();
            return LangType.Int;
        }

        Expect(TokenKind.Void);
        return LangType.Void;
    }

    private FunctionDeclaration ParseFunctionDeclaration(LangType type, Token name)
    {
        Expect(TokenKind.LeftParen);
        List<ParameterNode> parameters = new List<ParameterNode>();

        if (Current.Kind != TokenKind.RightParen)
        {
            parameters.Add(ParseParameter());

            while (Current.Kind == TokenKind.Comma)
            {
                Advance();
                parameters.Add(ParseParameter());
            }
        }

        Expect(TokenKind.RightParen);
        BlockStatement body = ParseBlock();
        return new FunctionDeclaration(Position(name), type, name.Lexeme, parameters, body);
    }

    private ParameterNode ParseParameter()
    {
        Expect(TokenKind.Int);
        Token name = ExpectIdentifier();
        return new ParameterNode(Position(name), LangType.Int, name.Lexeme);
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
        if (Current.Kind == TokenKind.Int)
        {
            return ParseVariableDeclarationStatement();
        }

        if (Current.Kind == TokenKind.Return)
        {
            return ParseReturnStatement();
        }

        if (Current.Kind == TokenKind.LeftBrace)
        {
            return ParseBlock();
        }

        if (Current.Kind == TokenKind.Identifier && NextKind == TokenKind.Assign)
        {
            return ParseAssignmentStatement();
        }

        AstPosition position = Position(Current);
        Expression expression = ParseExpression();
        Expect(TokenKind.Semicolon);
        return new ExpressionStatement(position, expression);
    }

    private VariableDeclarationStatement ParseVariableDeclarationStatement()
    {
        Expect(TokenKind.Int);
        Token name = ExpectIdentifier();
        Expect(TokenKind.Assign);
        Expression initializer = ParseExpression();
        Expect(TokenKind.Semicolon);

        VariableDeclaration declaration = new VariableDeclaration(
            Position(name), LangType.Int, name.Lexeme, initializer);

        return new VariableDeclarationStatement(Position(name), declaration);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        Token name = ExpectIdentifier();
        Expect(TokenKind.Assign);
        Expression value = ParseExpression();
        Expect(TokenKind.Semicolon);
        return new AssignmentStatement(Position(name), name.Lexeme, value);
    }

    private ReturnStatement ParseReturnStatement()
    {
        Token keyword = Current;
        Expect(TokenKind.Return);
        Expression? value = Current.Kind == TokenKind.Semicolon ? null : ParseExpression();
        Expect(TokenKind.Semicolon);
        return new ReturnStatement(Position(keyword), value);
    }

    private Expression ParseExpression()
    {
        return ParseAdditive(allowIdentifiers: true);
    }

    private Expression ParseGlobalInitializer()
    {
        return ParseAdditive(allowIdentifiers: false);
    }

    private Expression ParseAdditive(bool allowIdentifiers)
    {
        Expression left = ParseMultiplicative(allowIdentifiers);

        while (Current.Kind == TokenKind.Plus || Current.Kind == TokenKind.Minus)
        {
            Token op = Current;
            Advance();
            Expression right = ParseMultiplicative(allowIdentifiers);

            BinaryOperator kind = op.Kind == TokenKind.Plus
                ? BinaryOperator.Add
                : BinaryOperator.Subtract;

            left = new BinaryExpression(Position(op), kind, left, right);
        }

        return left;
    }

    private Expression ParseMultiplicative(bool allowIdentifiers)
    {
        Expression left = ParseUnary(allowIdentifiers);

        while (Current.Kind == TokenKind.Star
            || Current.Kind == TokenKind.Slash
            || Current.Kind == TokenKind.Percent)
        {
            Token op = Current;
            Advance();
            Expression right = ParseUnary(allowIdentifiers);

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

    private Expression ParseUnary(bool allowIdentifiers)
    {
        if (Current.Kind == TokenKind.Plus || Current.Kind == TokenKind.Minus)
        {
            Token op = Current;
            Advance();
            Expression operand = ParseUnary(allowIdentifiers);

            UnaryOperator kind = op.Kind == TokenKind.Plus
                ? UnaryOperator.Plus
                : UnaryOperator.Negate;

            return new UnaryExpression(Position(op), kind, operand);
        }

        return ParsePrimary(allowIdentifiers);
    }

    private Expression ParsePrimary(bool allowIdentifiers)
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
            Expression expression = ParseAdditive(allowIdentifiers);
            Expect(TokenKind.RightParen);
            return expression;
        }

        if (Current.Kind == TokenKind.Identifier)
        {
            if (!allowIdentifiers)
            {
                throw new ParserException(
                    "Identifiers and function calls are not allowed in global initializers",
                    token.Position);
            }

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

    private Token ExpectIdentifier()
    {
        Token token = Current;

        if (token.Kind != TokenKind.Identifier)
        {
            throw new ParserException(
                $"Expected identifier, got {token.Kind} '{token.Lexeme}'",
                token.Position);
        }

        Advance();
        return token;
    }

    private static AstPosition Position(Token token)
    {
        return new AstPosition(token.Position.Line, token.Position.Column);
    }
}