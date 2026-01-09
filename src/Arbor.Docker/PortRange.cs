using System;

namespace Arbor.Docker;

public readonly struct PortRange
{
    public int Start { get; }

    public int End { get; }

    public PortRange(int start, int end)
    {
        if (start <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Start must be greater than 0");
        }

        if (end <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than 0");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "End must be greater or equal to start");
        }

        Start = start;
        End = end;
        Length = end - start + 1;
    }

    public PortRange(int start)
    {
        if (start <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Start must be greater than 0");
        }

        Start = start;
        End = start;
        Length = 1;
    }

    public int Length { get; }
    public override string ToString() => End > Start ? $"{Start}-{End}" : Start.ToString();
}