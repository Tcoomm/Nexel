namespace Nexel.Vm;

internal enum VmValueKind
{
    Int,
    Bool,
    Double,
}

internal readonly record struct VmValue
{
    private VmValue(VmValueKind kind, int intValue, double doubleValue)
    {
        Kind = kind;
        IntValue = intValue;
        DoubleValue = doubleValue;
    }

    public VmValueKind Kind { get; }

    public int IntValue { get; }

    public double DoubleValue { get; }

    public bool BoolValue => IntValue != 0;

    public static VmValue FromInt(int value) => new VmValue(VmValueKind.Int, value, 0.0);

    public static VmValue FromBool(bool value) => new VmValue(VmValueKind.Bool, value ? 1 : 0, 0.0);

    public static VmValue FromDouble(double value) => new VmValue(VmValueKind.Double, 0, value);
}