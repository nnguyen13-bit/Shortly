using Shortly.Infrastructure.CodeGeneration;

namespace Shortly.Infrastructure.Tests.CodeGeneration;

public sealed class CounterRangeTests
{
    // --- Initial state ---

    [Fact]
    public void TryGetNext_BeforeReset_ReturnsFalse()
    {
        var range = new CounterRange();

        var result = range.TryGetNext(out var value);

        Assert.False(result);
        Assert.Equal(0, value);
    }

    // --- After Reset ---

    [Fact]
    public void TryGetNext_AfterReset_ReturnsFirstValue()
    {
        var range = new CounterRange();
        range.Reset(100, 105);

        var result = range.TryGetNext(out var value);

        Assert.True(result);
        Assert.Equal(100, value);
    }

    [Fact]
    public void TryGetNext_SequentialCalls_ReturnsIncrementingValues()
    {
        var range = new CounterRange();
        range.Reset(10, 12);

        range.TryGetNext(out var v1);
        range.TryGetNext(out var v2);
        range.TryGetNext(out var v3);

        Assert.Equal(10, v1);
        Assert.Equal(11, v2);
        Assert.Equal(12, v3);
    }

    [Fact]
    public void TryGetNext_ExhaustsRange_ReturnsFalse()
    {
        var range = new CounterRange();
        range.Reset(10, 12);

        range.TryGetNext(out _); // 10
        range.TryGetNext(out _); // 11
        range.TryGetNext(out _); // 12

        var result = range.TryGetNext(out var value);

        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetNext_SingleValueRange_ReturnsOneValue()
    {
        var range = new CounterRange();
        range.Reset(42, 42);

        Assert.True(range.TryGetNext(out var value));
        Assert.Equal(42, value);

        Assert.False(range.TryGetNext(out _));
    }

    // --- Reset behaviour ---

    [Fact]
    public void Reset_AfterExhaustion_AllowsNewValues()
    {
        var range = new CounterRange();
        range.Reset(1, 2);

        range.TryGetNext(out _);
        range.TryGetNext(out _);
        Assert.False(range.TryGetNext(out _));

        range.Reset(100, 102);

        Assert.True(range.TryGetNext(out var value));
        Assert.Equal(100, value);
    }

    [Fact]
    public void Reset_MidRange_DiscardsRemainingValues()
    {
        var range = new CounterRange();
        range.Reset(1, 100);

        range.TryGetNext(out var first);
        Assert.Equal(1, first);

        range.Reset(500, 502);

        range.TryGetNext(out var afterReset);
        Assert.Equal(500, afterReset);
    }

    // --- Range size ---

    [Fact]
    public void TryGetNext_FullRangeOf1000_ReturnsAllValues()
    {
        var range = new CounterRange();
        range.Reset(1, 1000);

        var values = new List<long>();
        while (range.TryGetNext(out var value))
        {
            values.Add(value);
        }

        Assert.Equal(1000, values.Count);
        Assert.Equal(1, values.First());
        Assert.Equal(1000, values.Last());
    }

    [Fact]
    public void TryGetNext_AllValuesAreUnique()
    {
        var range = new CounterRange();
        range.Reset(100_000_000, 100_001_000);

        var seen = new HashSet<long>();
        while (range.TryGetNext(out var value))
        {
            Assert.True(seen.Add(value), $"Duplicate value: {value}");
        }

        Assert.Equal(1001, seen.Count);
    }

    // --- Thread safety ---

    [Fact]
    public void TryGetNext_ConcurrentAccess_NoDuplicates()
    {
        var range = new CounterRange();
        range.Reset(1, 10_000);

        var allValues = new System.Collections.Concurrent.ConcurrentBag<long>();

        Parallel.For(0, 10_000, _ =>
        {
            if (range.TryGetNext(out var value))
            {
                allValues.Add(value);
            }
        });

        Assert.Equal(allValues.Count, allValues.Distinct().Count());
        Assert.Equal(10_000, allValues.Count);
    }

    [Fact]
    public void TryGetNext_ConcurrentAccess_NoValueExceedsEnd()
    {
        var range = new CounterRange();
        range.Reset(1, 5000);

        var allValues = new System.Collections.Concurrent.ConcurrentBag<long>();

        Parallel.For(0, 10_000, _ =>
        {
            if (range.TryGetNext(out var value))
            {
                allValues.Add(value);
            }
        });

        Assert.Equal(5000, allValues.Count);
        Assert.All(allValues, v => Assert.InRange(v, 1, 5000));
    }
}
