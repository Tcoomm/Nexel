namespace Nexel.AcceptanceTests;

using Nexel.Execution;

internal static class InterpreterTestRunner
{
    public static string Run(string source, string input = "")
    {
        using StringReader reader = new StringReader(input);
        using StringWriter writer = new StringWriter();

        new Interpreter().Execute(source, reader, writer);

        return writer.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}