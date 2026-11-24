namespace Nexel.Ast;

public sealed class IfStatement : Statement
{
    public IfStatement(
        AstPosition position,
        Expression condition,
        BlockStatement thenBlock,
        BlockStatement? elseBlock)
        : base(position)
    {
        Condition = condition;
        ThenBlock = thenBlock;
        ElseBlock = elseBlock;
    }

    public Expression Condition { get; }

    public BlockStatement ThenBlock { get; }

    public BlockStatement? ElseBlock { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}