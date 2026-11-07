namespace Nexel.Execution;

using System.IO;

using Nexel.Ast;
using Nexel.Lexer;
using Nexel.Parser;
using Nexel.Vm;
using Nexel.VmCodegen;

public sealed class Interpreter
{
    public void Execute(string source, TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        IReadOnlyList<Token> tokens = new Lexer(source).Tokenize();
        ProgramNode programNode = new Parser(tokens).ParseProgram();
        VmProgram vmProgram = new VmCodeGenerator().Generate(programNode);

        new VirtualMachine().Execute(vmProgram, input, output);
    }
}