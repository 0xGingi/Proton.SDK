namespace Proton.Sdk;

internal static class JitterGenerator
{
    private static readonly Random Random = new();

    public static TimeSpan ApplyJitter(TimeSpan interval, double maxDeviation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(interval.Ticks, nameof(interval));
        ArgumentOutOfRangeException.ThrowIfNegative(maxDeviation, nameof(maxDeviation));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(maxDeviation, 1, nameof(maxDeviation));

        return interval + TimeSpan.FromMilliseconds(interval.TotalMilliseconds * maxDeviation * ((2.0 * Random.NextDouble()) - 1.0));
    }
}
