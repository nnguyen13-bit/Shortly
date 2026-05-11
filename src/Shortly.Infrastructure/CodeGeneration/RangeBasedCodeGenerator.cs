using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Shortly.Application.Interfaces;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Configuration;

namespace Shortly.Infrastructure.CodeGeneration;

public sealed class RangeBasedCodeGenerator : IShortCodeGenerator
{
    private readonly IMongoCollection<CounterDocument> _counters;
    private readonly ConcurrentDictionary<string, CounterRange> _ranges = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _claimLocks = new();
    private const int RangeSize = 1000;
    private const long InitialCounterValue = 100_000_000;

    public RangeBasedCodeGenerator(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _counters = database.GetCollection<CounterDocument>("counters");
    }

    public async Task<ShortCode> GenerateAsync(DomainPrefix prefix, CancellationToken cancellationToken = default)
    {
        var range = _ranges.GetOrAdd(prefix.Value, _ => new CounterRange());

        if (range.TryGetNext(out var value))
        {
            return new ShortCode(Base62.Encode(value));
        }

        var claimLock = _claimLocks.GetOrAdd(prefix.Value, _ => new SemaphoreSlim(1, 1));
        await claimLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock — another thread may have claimed already
            if (range.TryGetNext(out value))
            {
                return new ShortCode(Base62.Encode(value));
            }

            var newEnd = await ClaimRangeAsync(prefix.Value, cancellationToken);
            range.Reset(newEnd - RangeSize + 1, newEnd);
            range.TryGetNext(out value);
            return new ShortCode(Base62.Encode(value));
        }
        finally
        {
            claimLock.Release();
        }
    }

    private async Task<long> ClaimRangeAsync(string prefix, CancellationToken cancellationToken)
    {
        var filter = Builders<CounterDocument>.Filter.Eq(c => c.Id, prefix);
        var update = Builders<CounterDocument>.Update.Inc(c => c.CurrentValue, RangeSize);
        var options = new FindOneAndUpdateOptions<CounterDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _counters.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

        // If this is the first claim (counter was just created), set to initial value
        if (result.CurrentValue == RangeSize)
        {
            var setInitial = Builders<CounterDocument>.Update.Set(c => c.CurrentValue, InitialCounterValue + RangeSize);
            result = await _counters.FindOneAndUpdateAsync(filter, setInitial, options, cancellationToken);
            return result.CurrentValue;
        }

        return result.CurrentValue;
    }
}

internal sealed class CounterDocument
{
    [BsonId]
    public string Id { get; set; } = null!;
    public long CurrentValue { get; set; }
}

internal sealed class CounterRange
{
    private long _current;
    private long _end;
    private readonly Lock _lock = new();

    public bool TryGetNext(out long value)
    {
        lock (_lock)
        {
            if (_current > 0 && _current <= _end)
            {
                value = _current++;
                return true;
            }
            value = 0;
            return false;
        }
    }

    public void Reset(long start, long end)
    {
        lock (_lock)
        {
            _current = start;
            _end = end;
        }
    }
}
