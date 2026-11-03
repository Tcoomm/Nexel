namespace Nexel.Ast;

public interface IAstVisitor
{
    void Visit(ProgramNode node);

    void Visit(FunctionDeclaration node);

    void Visit(VariableDeclaration node);

    void Visit(ParameterNode node);

    void Visit(BlockStatement node);

    void Visit(VariableDeclarationStatement node);

    void Visit(AssignmentStatement node);

    void Visit(ExpressionStatement node);

    void Visit(ReturnStatement node);

    void Visit(IntegerLiteralExpression node);

    void Visit(VariableExpression node);

    void Visit(UnaryExpression node);

    void Visit(BinaryExpression node);

    void Visit(CallExpression node);
}