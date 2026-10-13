namespace Nexel.Vm;

#pragma warning disable RCS1194

public sealed class VmException : Exception
{
    public VmException(string message)
        : base(message)
    {
    }

    public VmException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

#pragma warning restore RCS1194