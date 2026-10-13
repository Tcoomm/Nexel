using Xunit;

namespace Nexel.Vm.UnitTests;

public sealed class VirtualMachineTests
{
    [Fact]
    public void Push_ShouldPlaceValueOnStack()
    {
        VirtualMachine virtualMachine = new VirtualMachine();
        StringWriter output = new StringWriter();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Push, 42),
            new Instruction(OpCode.Print),
        };

        virtualMachine.Execute(program, new StringReader(string.Empty), output);

        Assert.Equal("42" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Add_ShouldAddTwoValues()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 10),
                new Instruction(OpCode.Push, 2),
                new Instruction(OpCode.Add),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("12" + Environment.NewLine, output);
    }

    [Fact]
    public void Sub_ShouldUseCorrectOperandOrder()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 10),
                new Instruction(OpCode.Push, 3),
                new Instruction(OpCode.Sub),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("7" + Environment.NewLine, output);
    }

    [Fact]
    public void Mul_ShouldMultiplyTwoValues()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 10),
                new Instruction(OpCode.Push, 5),
                new Instruction(OpCode.Mul),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("50" + Environment.NewLine, output);
    }

    [Fact]
    public void Div_ShouldUseCorrectOperandOrder()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 20),
                new Instruction(OpCode.Push, 5),
                new Instruction(OpCode.Div),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("4" + Environment.NewLine, output);
    }

    [Fact]
    public void Mod_ShouldUseCorrectOperandOrder()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 17),
                new Instruction(OpCode.Push, 5),
                new Instruction(OpCode.Mod),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("2" + Environment.NewLine, output);
    }

    [Fact]
    public void Neg_ShouldNegateValue()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 5),
                new Instruction(OpCode.Neg),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("-5" + Environment.NewLine, output);
    }

    [Fact]
    public void Pop_ShouldRemoveValueFromStack()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Push, 42),
            new Instruction(OpCode.Pop),
        };

        virtualMachine.Execute(
            program,
            new StringReader(string.Empty),
            new StringWriter());
    }

    [Fact]
    public void Input_ShouldReadInteger()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.ReadInt),
                new Instruction(OpCode.Push, 1),
                new Instruction(OpCode.Add),
                new Instruction(OpCode.Print),
            },
            "7" + Environment.NewLine);

        Assert.Equal("8" + Environment.NewLine, output);
    }

    [Fact]
    public void Print_ShouldWriteOneLine()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 123),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("123" + Environment.NewLine, output);
    }

    [Fact]
    public void CompoundExpression_ShouldProduceTwenty()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 10),
                new Instruction(OpCode.Push, 2),
                new Instruction(OpCode.Push, 5),
                new Instruction(OpCode.Mul),
                new Instruction(OpCode.Add),
                new Instruction(OpCode.Print),
            });

        Assert.Equal("20" + Environment.NewLine, output);
    }

    [Fact]
    public void EmptyProgram_ShouldCompleteSuccessfully()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        virtualMachine.Execute(
            new List<Instruction>(),
            new StringReader(string.Empty),
            new StringWriter());
    }

    [Fact]
    public void MultiplePrint_ShouldWriteSeparateLines()
    {
        string output = ExecuteAndGetOutput(
            new List<Instruction>
            {
                new Instruction(OpCode.Push, 10),
                new Instruction(OpCode.Print),
                new Instruction(OpCode.Push, 20),
                new Instruction(OpCode.Print),
            });

        Assert.Equal(
            "10" + Environment.NewLine + "20" + Environment.NewLine,
            output);
    }

    [Fact]
    public void BinaryInstruction_ShouldThrowOnStackUnderflow()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Add),
        };

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                program,
                new StringReader(string.Empty),
                new StringWriter()));
    }

    [Fact]
    public void Div_ShouldThrowOnZero()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 0),
            new Instruction(OpCode.Div),
        };

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                program,
                new StringReader(string.Empty),
                new StringWriter()));
    }

    [Fact]
    public void Mod_ShouldThrowOnZero()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 0),
            new Instruction(OpCode.Mod),
        };

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                program,
                new StringReader(string.Empty),
                new StringWriter()));
    }

    [Fact]
    public void Input_ShouldThrowOnEndOfInput()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                new List<Instruction> { new Instruction(OpCode.ReadInt) },
                new StringReader(string.Empty),
                new StringWriter()));
    }

    [Fact]
    public void Input_ShouldThrowOnInvalidInteger()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                new List<Instruction> { new Instruction(OpCode.ReadInt) },
                new StringReader("abc" + Environment.NewLine),
                new StringWriter()));
    }

    [Fact]
    public void Neg_ShouldThrowOnEmptyStack()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                new List<Instruction> { new Instruction(OpCode.Neg) },
                new StringReader(string.Empty),
                new StringWriter()));
    }

    [Fact]
    public void ArithmeticOverflow_ShouldThrowVmException()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Push, int.MaxValue),
            new Instruction(OpCode.Push, 1),
            new Instruction(OpCode.Add),
        };

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                program,
                new StringReader(string.Empty),
                new StringWriter()));
    }

    [Fact]
    public void NegatingMinValue_ShouldThrowVmException()
    {
        VirtualMachine virtualMachine = new VirtualMachine();

        List<Instruction> program = new List<Instruction>
        {
            new Instruction(OpCode.Push, int.MinValue),
            new Instruction(OpCode.Neg),
        };

        Assert.Throws<VmException>(() =>
            virtualMachine.Execute(
                program,
                new StringReader(string.Empty),
                new StringWriter()));
    }

    private static string ExecuteAndGetOutput(
        IReadOnlyList<Instruction> program,
        string input = "")
    {
        VirtualMachine virtualMachine = new VirtualMachine();
        StringWriter output = new StringWriter();

        virtualMachine.Execute(
            program,
            new StringReader(input),
            output);

        return output.ToString();
    }
}
