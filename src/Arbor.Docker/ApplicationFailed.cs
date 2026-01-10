using System;

namespace Arbor.Docker;

public sealed class ApplicationFailed : IApplicationEvent
{
    public Exception Exception { get; }

    internal ApplicationFailed(Exception ex) => Exception = ex;
}