namespace Nexel.Semantics;

using Nexel.Ast;

public abstract class AbstractPass : IAstVisitor
{
    public virtual void Visit(ProgramNode node)
    {
        foreach (Declaration declaration in node.Declarations)
        {
            declaration.Accept(this);
        }
    }

    public virtual void Visit(FunctionDeclaration node)
    {
        foreach (ParameterNode parameter in node.Parameters)
        {
            parameter.Accept(this);
        }

        node.Body.Accept(this);
    }

    public virtual void Visit(VariableDeclaration node)
    {
        node.Initializer.Accept(this);
    }

    public virtual void Visit(ParameterNode node)
    {
    }

    public virtual void Visit(BlockStatement node)
    {
        foreach (Statement statement in node.Statements)
        {
            statement.Accept(this);
        }
    }

    public virtual void Visit(VariableDeclarationStatement node)
    {
        node.Declaration.Accept(this);
    }

    public virtual void Visit(AssignmentStatement node)
    {
        node.Value.Accept(this);
    }

    public virtual void Visit(ExpressionStatement node)
    {
        node.Expression.Accept(this);
    }

    public virtual void Visit(ReturnStatement node)
    {
        node.Value?.Accept(this);
    }

    public virtual void Visit(IntegerLiteralExpression node)
    {
    }

    public virtual void Visit(VariableExpression node)
    {
    }

    public virtual void Visit(UnaryExpression node)
    {
        node.Operand.Accept(this);
    }

    public virtual void Visit(BinaryExpression node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
    }

    public virtual void Visit(CallExpression node)
    {
        foreach (Expression argument in node.Arguments)
        {
            argument.Accept(this);
        }
    }
}