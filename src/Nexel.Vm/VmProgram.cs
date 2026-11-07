namespace Nexel.Vm;

public sealed record VmProgram(
    IReadOnlyList<Instruction> Instructions,
    IReadOnlyList<VmFunction> Functions,
    int GlobalCount,
    int EntryFunctionIndex);