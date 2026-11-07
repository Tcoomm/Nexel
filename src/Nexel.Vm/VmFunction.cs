namespace Nexel.Vm;

public sealed record VmFunction(
    string Name,
    int EntryPoint,
    int ParameterCount,
    int LocalCount,
    bool ReturnsValue);
