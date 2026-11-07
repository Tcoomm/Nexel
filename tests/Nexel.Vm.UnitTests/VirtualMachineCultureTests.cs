namespace Nexel.Vm.UnitTests;

using System.Globalization;

using Xunit;

public sealed class VirtualMachineCultureTests
{
    [Fact]
    public void Print_UsesInvariantCulture()
    {
        CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";

        Instruction[] instructions =
        {
            new Instruction(OpCode.Push, -5),
            new Instruction(OpCode.Print),
        };

        using StringReader input = new StringReader(string.Empty);
        using StringWriter output = new StringWriter(culture);

        new VirtualMachine().Execute(instructions, input, output);

        Assert.Equal("-5" + Environment.NewLine, output.ToString());
    }
}