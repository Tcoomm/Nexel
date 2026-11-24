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

        if (type != LangType.Void && Current.Kind == TokenKind.Assign)
        {
            Advance();
            Expression initializer = ParseGlobalInitializer();
            Expect(TokenKind.Semicolon);
            return new VariableDeclaration(Position(name), type, name.Lexeme, initializer);
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

        if (Current.Kind == TokenKind.Bool)
        {
            Advance();
            return LangType.Bool;
        }

        if (Current.Kind == TokenKind.Double)
        {
            Advance();
            return LangType.Double;
        }

        Expect(TokenKind.Void);
        return LangType.Void;
    }

    private LangType ParseValueType()
    {
        if (Current.Kind == TokenKind.Int)
        {
            Advance();
            return LangType.Int;
        }

        if (Current.Kind == TokenKind.Bool)
        {
            Advance();
            return LangType.Bool;
        }

        if (Current.Kind == TokenKind.Double)
        {
            Advance();
            return LangType.Double;
        }

        throw new ParserException(
            $"Expected type 'int', 'bool', or 'double', got {Current.Kind} '{Current.Lexeme}'",
            Current.Position);
    }

    private static bool IsValueTypeStart(TokenKind kind) =>
        kind == TokenKind.Int || kind == TokenKind.Bool || kind == TokenKind.Double;

    private static bool IsAssignmentOperator(TokenKind kind) =>
        kind == TokenKind.Assign
        || kind == TokenKind.PlusAssign
        || kind == TokenKind.MinusAssign
        || kind == TokenKind.StarAssign
        || kind == TokenKind.SlashAssign;

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
        LangType type = ParseValueType();
        Token name = ExpectIdentifier();
        return new ParameterNode(Position(name), type, name.Lexeme);
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
        if (IsValueTypeStart(Current.Kind))
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

        if (Current.Kind == TokenKind.If)
        {
            return ParseIfStatement();
        }

        if (Current.Kind == TokenKind.While)
        {
            return ParseWhileStatement();
        }

        if (Current.Kind == TokenKind.For)
        {
            return ParseForStatement();
        }

        if (Current.Kind == TokenKind.Identifier && IsAssignmentOperator(NextKind))
        {
            return ParseAssignmentStatement();
        }

        AstPosition position = Position(Current);
        Expression expression = ParseExpression();
        Expect(TokenKind.Semicolon);
        return new ExpressionStatement(position, expression);
    }

    private IfStatement ParseIfStatement()
    {
        Token keyword = Current;
        Expect(TokenKind.If);
        Expect(TokenKind.LeftParen);
        Expression condition = ParseExpression();
        Expect(TokenKind.RightParen);
        BlockStatement thenBlock = ParseBlock();

        BlockStatement? elseBlock = null;
        if (Current.Kind == TokenKind.Else)
        {
            Advance();
            elseBlock = ParseBlock();
        }

        return new IfStatement(Position(keyword), condition, thenBlock, elseBlock);
    }

    private WhileStatement ParseWhileStatement()
    {
        Token keyword = Current;
        Expect(TokenKind.While);
        Expect(TokenKind.LeftParen);
        Expression condition = ParseExpression();
        Expect(TokenKind.RightParen);
        BlockStatement body = ParseBlock();
        return new WhileStatement(Position(keyword), condition, body);
    }

    private ForStatement ParseForStatement()
    {
        Token keyword = Current;
        Expect(TokenKind.For);
        Expect(TokenKind.LeftParen);

        Statement initializer = ParseForInitializer();
        Expect(TokenKind.Semicolon);

        Expression condition = ParseExpression();
        Expect(TokenKind.Semicolon);

        AssignmentStatement step = ParseForStep();
        Expect(TokenKind.RightParen);

        BlockStatement body = ParseBlock();
        return new ForStatement(Position(keyword), initializer, condition, step, body);
    }

    private Statement ParseForInitializer()
    {
        if (IsValueTypeStart(Current.Kind))
        {
            LangType type = ParseValueType();
            Token name = ExpectIdentifier();
            Expect(TokenKind.Assign);
            Expression initializer = ParseExpression();

            VariableDeclaration declaration = new VariableDeclaration(
                Position(name), type, name.Lexeme, initializer);

            return new VariableDeclarationStatement(Position(name), declaration);
        }

        return ParseAssignment();
    }

    private AssignmentStatement ParseForStep()
    {
        return ParseAssignment();
    }

    private VariableDeclarationStatement ParseVariableDeclarationStatement()
    {
        LangType type = ParseValueType();
        Token name = ExpectIdentifier();
        Expect(TokenKind.Assign);
        Expression initializer = ParseExpression();
        Expect(TokenKind.Semicolon);

        VariableDeclaration declaration = new VariableDeclaration(
            Position(name), type, name.Lexeme, initializer);

        return new VariableDeclarationStatement(Position(name), declaration);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        AssignmentStatement assignment = ParseAssignment();
        Expect(TokenKind.Semicolon);
        return assignment;
    }

    private AssignmentStatement ParseAssignment()
    {
        Token identifier = ExpectIdentifier();
        AssignmentOperator op = ParseAssignmentOperator();
        Expression value = ParseExpression();
        return new AssignmentStatement(Position(identifier), identifier.Lexeme, value, op);
    }

    private AssignmentOperator ParseAssignmentOperator()
    {
        Token opToken = Current;

        AssignmentOperator op = opToken.Kind switch
        {
            TokenKind.Assign => AssignmentOperator.Assign,
            TokenKind.PlusAssign => AssignmentOperator.Add,
            TokenKind.MinusAssign => AssignmentOperator.Subtract,
            TokenKind.StarAssign => AssignmentOperator.Multiply,
            TokenKind.SlashAssign => AssignmentOperator.Divide,
            _ => throw new ParserException(
                $"Expected assignment operator, got {opToken.Kind} '{opToken.Lexeme}'",
                opToken.Position),
        };

        Advance();
        return op;
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
        return ParseOr(allowIdentifiers: true);
    }

    private Expression ParseGlobalInitializer()
    {
        return ParseOr(allowIdentifiers: false);
    }

    private Expression ParseOr(bool allowIdentifiers)
    {
        Expression left = ParseAnd(allowIdentifiers);

        while (Current.Kind == TokenKind.Or)
        {
            Token op = Current;
            Advance();
            Expression right = ParseAnd(allowIdentifiers);
            left = new BinaryExpression(Position(op), BinaryOperator.Or, left, right);
        }

        return left;
    }

    private Expression ParseAnd(bool allowIdentifiers)
    {
        Expression left = ParseEquality(allowIdentifiers);

        while (Current.Kind == TokenKind.And)
        {
            Token op = Current;
            Advance();
            Expression right = ParseEquality(allowIdentifiers);
            left = new BinaryExpression(Position(op), BinaryOperator.And, left, right);
        }

        return left;
    }

    private Expression ParseEquality(bool allowIdentifiers)
    {
        Expression left = ParseRelational(allowIdentifiers);

        while (Current.Kind == TokenKind.Equal || Current.Kind == TokenKind.NotEqual)
        {
            Token op = Current;
            Advance();
            Expression right = ParseRelational(allowIdentifiers);

            BinaryOperator kind = op.Kind == TokenKind.Equal
                ? BinaryOperator.Equal
                : BinaryOperator.NotEqual;

            left = new BinaryExpression(Position(op), kind, left, right);
        }

        return left;
    }

    private Expression ParseRelational(bool allowIdentifiers)
    {
        Expression left = ParseAdditive(allowIdentifiers);

        while (Current.Kind == TokenKind.Less
            || Current.Kind == TokenKind.LessOrEqual
            || Current.Kind == TokenKind.Greater
            || Current.Kind == TokenKind.GreaterOrEqual)
        {
            Token op = Current;
            Advance();
            Expression right = ParseAdditive(allowIdentifiers);

            BinaryOperator kind = op.Kind switch
            {
                TokenKind.Less => BinaryOperator.Less,
                TokenKind.LessOrEqual => BinaryOperator.LessOrEqual,
                TokenKind.Greater => BinaryOperator.Greater,
                TokenKind.GreaterOrEqual => BinaryOperator.GreaterOrEqual,
                _ => throw new ParserException($"Unexpected operator {op.Kind}", op.Position),
            };

            left = new BinaryExpression(Position(op), kind, left, right);
        }

        return left;
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
        if (Current.Kind == TokenKind.Plus
            || Current.Kind == TokenKind.Minus
            || Current.Kind == TokenKind.Not)
        {
            Token op = Current;
            Advance();
            Expression operand = ParseUnary(allowIdentifiers);

            UnaryOperator kind = op.Kind switch
            {
                TokenKind.Plus => UnaryOperator.Plus,
                TokenKind.Minus => UnaryOperator.Negate,
                TokenKind.Not => UnaryOperator.Not,
                _ => throw new ParserException($"Unexpected unary operator {op.Kind}", op.Position),
            };

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

        if (Current.Kind == TokenKind.DoubleLiteral)
        {
            if (!double.TryParse(
                token.Lexeme,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value) || !double.IsFinite(value))
            {
                throw new ParserException(
                    $"Double literal is invalid or out of range: {token.Lexeme}",
                    token.Position);
            }

            Advance();
            return new DoubleLiteralExpression(Position(token), value);
        }

        if (Current.Kind == TokenKind.True || Current.Kind == TokenKind.False)
        {
            bool value = Current.Kind == TokenKind.True;
            Advance();
            return new BooleanLiteralExpression(Position(token), value);
        }

        if (Current.Kind == TokenKind.LeftParen)
        {
            Advance();
            Expression expression = ParseOr(allowIdentifiers);
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