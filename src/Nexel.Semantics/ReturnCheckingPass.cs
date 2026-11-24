namespace Nexel.Semantics;

using Nexel.Ast;

internal sealed class ReturnCheckingPass : AbstractPass
{
    public override void Visit(FunctionDeclaration node)
    {
        if (node.ReturnType != LangType.Void && !GuaranteesReturn(node.Body))
        {
            throw new SemanticException($"Function '{node.Name}' must guarantee a return statement", node.Position);
        }
    }

    private static bool GuaranteesReturn(BlockStatement block)
    {
        foreach (Statement statement in block.Statements)
        {
            if (statement is ReturnStatement)
            {
                return true;
            }

            if (statement is BlockStatement nested && GuaranteesReturn(nested))
            {
                return true;
            }

            // If гарантирует возврат только с else и обеими возвращающими ветвями.
            if (statement is IfStatement ifStatement
                && ifStatement.ElseBlock is not null
                && GuaranteesReturn(ifStatement.ThenBlock)
                && GuaranteesReturn(ifStatement.ElseBlock))
            {
                return true;
            }
        }

        return false;
    }
}