namespace Nexel.Execution;

using System.Collections.Generic;

using Nexel.Lexer;
using Nexel.Parser;
using Nexel.Vm;

public sealed class Interpreter
{
    public void Execute(string source, TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        IReadOnlyList<Token> tokens = new Lexer(source).Tokenize();
        IReadOnlyList<Instruction> instructions = new Parser(tokens).ParseProgram();
        new VirtualMachine().Execute(instructions, input, output);
    }
}