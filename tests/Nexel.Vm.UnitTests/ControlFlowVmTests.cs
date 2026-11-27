namespace Nexel.Vm.UnitTests;

using Xunit;

public sealed class ControlFlowVmTests
{
    [Fact]
    public void Jump_SkipsInstructions()
    {
        string output = Run(
            new Instruction(OpCode.Jump, 3),
            new Instruction(OpCode.Push, 99),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 42),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));

        Assert.Equal("42\n", output);
    }

    [Fact]
    public void JumpIfFalse_JumpsWhenFalse()
    {
        string output = Run(
            new Instruction(OpCode.PushBool, 0),
            new Instruction(OpCode.JumpIfFalse, 4),
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));

        Assert.Equal("2\n", output);
    }

    [Fact]
    public void JumpIfTrue_JumpsWhenTrue()
    {
        string output = Run(
            new Instruction(OpCode.PushBool, 1),
            new Instruction(OpCode.JumpIfTrue, 4),
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));

        Assert.Equal("2\n", output);
    }

    [Fact]
    public void JumpIfFalse_DoesNotJumpWhenTrue()
    {
        string output = Run(
            new Instruction(OpCode.PushBool, 1),
            new Instruction(OpCode.JumpIfFalse, 4),
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));

        Assert.Equal("1\n2\n", output);
    }

    [Fact]
    public void JumpIfTrue_DoesNotJumpWhenFalse()
    {
        string output = Run(
            new Instruction(OpCode.PushBool, 0),
            new Instruction(OpCode.JumpIfTrue, 4),
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));

        Assert.Equal("1\n2\n", output);
    }

    [Fact]
    public void JumpToOutOfBounds_ThrowsVmException()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Jump, 999),
            new Instruction(OpCode.Return),
        };

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(program, new StringReader(string.Empty), new StringWriter()));
    }

    [Fact]
    public void PushBool_NormalizesNonZeroToTrue()
    {
        string output = Run(
            new Instruction(OpCode.PushBool, 5),
            new Instruction(OpCode.JumpIfFalse, 5),
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return));

        Assert.Equal("1\n2\n", output);
    }

    private static string Run(params Instruction[] instructions)
    {
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 0, ReturnsValue: false);
        VmProgram program = new VmProgram(instructions, new[] { entry }, GlobalCount: 0, EntryFunctionIndex: 0);
        using StringReader reader = new StringReader(string.Empty);
        using StringWriter writer = new StringWriter();

        new VirtualMachine().Execute(program, reader, writer);

        return writer.ToString().Replace("\r\n", "\n");
    }
}