namespace Nexel.Vm.UnitTests;

using Xunit;

public sealed class VmProgramExecutionTests
{
    [Fact]
    public void LocalAndGlobalSlots_AreIsolatedAndDoNotCorruptEachOther()
    {
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 2, ReturnsValue: false);
        Instruction[] instructions =
        {
            new Instruction(OpCode.Push, 100),
            new Instruction(OpCode.StoreGlobal, 0),
            new Instruction(OpCode.Push, 200),
            new Instruction(OpCode.StoreLocal, 0),
            new Instruction(OpCode.Push, 300),
            new Instruction(OpCode.StoreLocal, 1),
            new Instruction(OpCode.LoadGlobal, 0),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.LoadLocal, 0),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.LoadLocal, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return),
        };

        VmProgram program = new VmProgram(instructions, new[] { entry }, GlobalCount: 1, EntryFunctionIndex: 0);
        Assert.Equal("100\n200\n300\n", RunProgram(program));
    }

    [Fact]
    public void Call_f_10_3_AssignsParametersInOrderAndReturns7()
    {
        // <entry> (0..3): Push 10, Push 3, Call 1, Print, Return
        // sub(a, b) (5..8): LoadLocal 0, LoadLocal 1, Sub, Return
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 0, ReturnsValue: false);
        VmFunction sub = new VmFunction("sub", EntryPoint: 5, ParameterCount: 2, LocalCount: 2, ReturnsValue: true);

        Instruction[] instructions =
        {
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 3),
            new Instruction(OpCode.Call, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return),
            new Instruction(OpCode.LoadLocal, 0),
            new Instruction(OpCode.LoadLocal, 1),
            new Instruction(OpCode.Sub),
            new Instruction(OpCode.Return),
        };

        VmProgram program = new VmProgram(instructions, new[] { entry, sub }, GlobalCount: 0, EntryFunctionIndex: 0);
        Assert.Equal("7\n", RunProgram(program));
    }

    [Fact]
    public void Call_100PlusF2_PreservesCallerStackBelowStackBase()
    {
        // <entry> (0..5): Push 100, Push 2, Call 1, Add, Print, Return
        // dbl(x) (6..9): LoadLocal 0, Push 2, Mul, Return
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 0, ReturnsValue: false);
        VmFunction dbl = new VmFunction("dbl", EntryPoint: 6, ParameterCount: 1, LocalCount: 1, ReturnsValue: true);

        Instruction[] instructions =
        {
            new Instruction(OpCode.Push, 100),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Call, 1),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return),
            new Instruction(OpCode.LoadLocal, 0),
            new Instruction(OpCode.Push, 2),
            new Instruction(OpCode.Mul),
            new Instruction(OpCode.Return),
        };

        VmProgram program = new VmProgram(instructions, new[] { entry, dbl }, GlobalCount: 0, EntryFunctionIndex: 0);
        Assert.Equal("104\n", RunProgram(program));
    }

    [Fact]
    public void TwoSequentialCalls_ReceiveFreshLocalFrames()
    {
        // fn(): LoadLocal 0 (default 0), Push 5, Add, StoreLocal 0, LoadLocal 0, Return
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 0, ReturnsValue: false);
        VmFunction fn = new VmFunction("fn", EntryPoint: 5, ParameterCount: 0, LocalCount: 1, ReturnsValue: true);

        Instruction[] instructions =
        {
            new Instruction(OpCode.Call, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Call, 1),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return),
            new Instruction(OpCode.LoadLocal, 0),
            new Instruction(OpCode.Push, 5),
            new Instruction(OpCode.Add),
            new Instruction(OpCode.StoreLocal, 0),
            new Instruction(OpCode.LoadLocal, 0),
            new Instruction(OpCode.Return),
        };

        VmProgram program = new VmProgram(instructions, new[] { entry, fn }, GlobalCount: 0, EntryFunctionIndex: 0);
        Assert.Equal("5\n5\n", RunProgram(program));
    }

    [Fact]
    public void VoidCall_FollowedByPrintNumber_LeavesStackClean()
    {
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 0, ReturnsValue: false);
        VmFunction noop = new VmFunction("noop", EntryPoint: 4, ParameterCount: 0, LocalCount: 0, ReturnsValue: false);

        Instruction[] instructions =
        {
            new Instruction(OpCode.Call, 1),
            new Instruction(OpCode.Push, 42),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return),
            new Instruction(OpCode.Return),
        };

        VmProgram program = new VmProgram(instructions, new[] { entry, noop }, GlobalCount: 0, EntryFunctionIndex: 0);
        Assert.Equal("42\n", RunProgram(program));
    }

    [Fact]
    public void AbsMinMax_ProduceExpectedResultsAndThrowOnMinValueAbs()
    {
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 0, ReturnsValue: false);
        Instruction[] instructions =
        {
            new Instruction(OpCode.Push, -15),
            new Instruction(OpCode.Abs),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 25),
            new Instruction(OpCode.Min),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Push, 10),
            new Instruction(OpCode.Push, 25),
            new Instruction(OpCode.Max),
            new Instruction(OpCode.Print),
            new Instruction(OpCode.Return),
        };

        VmProgram program = new VmProgram(instructions, new[] { entry }, GlobalCount: 0, EntryFunctionIndex: 0);
        Assert.Equal("15\n10\n25\n", RunProgram(program));

        VmProgram overflowAbs = new VmProgram(
            new[]
            {
                new Instruction(OpCode.Push, int.MinValue),
                new Instruction(OpCode.Abs),
                new Instruction(OpCode.Return),
            },
            new[] { entry },
            GlobalCount: 0,
            EntryFunctionIndex: 0);

        Assert.Throws<VmException>(() => RunProgram(overflowAbs));
    }

    [Fact]
    public void InvalidFunctionOrSlotIndex_AndUnbalancedReturn_ThrowVmException()
    {
        VmFunction entry = new VmFunction("<entry>", EntryPoint: 0, ParameterCount: 0, LocalCount: 1, ReturnsValue: false);

        // 1. Неизвестный индекс функции
        VmProgram badCall = new VmProgram(
            new[] { new Instruction(OpCode.Call, 99), new Instruction(OpCode.Return) },
            new[] { entry },
            GlobalCount: 0,
            EntryFunctionIndex: 0);
        Assert.Throws<VmException>(() => RunProgram(badCall));

        // 2. Неизвестный локальный слот
        VmProgram badLocal = new VmProgram(
            new[] { new Instruction(OpCode.LoadLocal, 5), new Instruction(OpCode.Return) },
            new[] { entry },
            GlobalCount: 0,
            EntryFunctionIndex: 0);
        Assert.Throws<VmException>(() => RunProgram(badLocal));

        // 3. Несбалансированный стек при Return (осталось лишнее число в void-функции)
        VmProgram unbalancedReturn = new VmProgram(
            new[] { new Instruction(OpCode.Push, 1), new Instruction(OpCode.Return) },
            new[] { entry },
            GlobalCount: 0,
            EntryFunctionIndex: 0);
        Assert.Throws<VmException>(() => RunProgram(unbalancedReturn));
    }

    private static string RunProgram(VmProgram program, string input = "")
    {
        using StringReader reader = new StringReader(input);
        using StringWriter writer = new StringWriter();

        new VirtualMachine().Execute(program, reader, writer);

        return writer.ToString().Replace("\r\n", "\n");
    }
}