namespace Nexel.Semantics;

using Nexel.Ast;

public static class SemanticAnalyzer
{
    public static void Analyze(ProgramNode program)
    {
        ArgumentNullException.ThrowIfNull(program);

        DeclarationPass declarations = new DeclarationPass();
        program.Accept(declarations);
        program.Accept(new TypeCheckingPass(declarations));
        program.Accept(new ReturnCheckingPass());
    }
}