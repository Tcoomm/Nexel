using Nexel.Lexer;
using Nexel.Parser;
using Nexel.Vm;

if (args.Length != 1)
{
    Console.Error.WriteLine("Укажите путь к исходному файлу.");
    return 1;
}

try
{
    string source = File.ReadAllText(args[0]);

    IReadOnlyList<Token> tokens = new Lexer(source).Tokenize();
    IReadOnlyList<Instruction> instructions =
        new Parser(tokens).ParseProgram();

    new VirtualMachine().Execute(
        instructions,
        Console.In,
        Console.Out);

    return 0;
}
catch (LexerException ex)
{
    Console.Error.WriteLine(ex.Message);
}
catch (ParserException ex)
{
    Console.Error.WriteLine(ex.Message);
}
catch (VmException ex)
{
    Console.Error.WriteLine(ex.Message);
}
catch (IOException ex)
{
    Console.Error.WriteLine(ex.Message);
}
catch (UnauthorizedAccessException ex)
{
    Console.Error.WriteLine(ex.Message);
}
catch (OverflowException ex)
{
    Console.Error.WriteLine(ex.Message);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
}

return 1;