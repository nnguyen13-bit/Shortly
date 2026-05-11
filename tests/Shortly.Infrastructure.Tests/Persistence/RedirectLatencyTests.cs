using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Configuration;
using Shortly.Infrastructure.Persistence;
using Shortly.Infrastructure.Persistence.Documents;
using Testcontainers.MongoDb;

namespace Shortly.Infrastructure.Tests.Persistence;

public sealed class RedirectLatencyTests : IAsyncLifetime
{
    private MongoDbContainer _container = null!;
    private MongoDbContext _context = null!;
    private MongoLinkRepository _repository = null!;

    private readonly List<(string Prefix, string Code)> _testLinks = [];

    public async Task InitializeAsync()
    {
        _container = new MongoDbBuilder("mongo:7").Build();
        await _container.StartAsync();

        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _container.GetConnectionString(),
            DatabaseName = "shortly_perf_test"
        });

        _context = new MongoDbContext(settings);
        _repository = new MongoLinkRepository(_context, TestResiliencePipeline.Provider);

        // Create indexes
        var indexInitialiser = new MongoDbIndexInitialiser(_context, NullLogger<MongoDbIndexInitialiser>.Instance);
        await indexInitialiser.StartAsync(CancellationToken.None);

        // Seed 10,000 links
        await SeedLinksAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task RedirectLookup_10000Links_P99Under100ms()
    {
        // Warmup — 100 lookups to prime connection pool and JIT
        for (var i = 0; i < 100; i++)
        {
            var (prefix, code) = _testLinks[i % _testLinks.Count];
            await _repository.GetByPrefixAndCodeAsync(new DomainPrefix(prefix), new ShortCode(code));
        }

        // Measure — 1,000 lookups on random links from the 10,000 set
        var random = new Random(42);
        var latencies = new List<double>(1000);

        for (var i = 0; i < 1000; i++)
        {
            var (prefix, code) = _testLinks[random.Next(_testLinks.Count)];

            var sw = Stopwatch.StartNew();
            var link = await _repository.GetByPrefixAndCodeAsync(new DomainPrefix(prefix), new ShortCode(code));
            sw.Stop();

            Assert.NotNull(link);
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        latencies.Sort();
        var p50 = latencies[(int)(latencies.Count * 0.50)];
        var p95 = latencies[(int)(latencies.Count * 0.95)];
        var p99 = latencies[(int)(latencies.Count * 0.99)];
        var max = latencies[^1];

        // Output latency stats for visibility
        Console.WriteLine($"Redirect latency (ms) — P50: {p50:F2}, P95: {p95:F2}, P99: {p99:F2}, Max: {max:F2}");

        Assert.True(p99 < 100, $"P99 latency {p99:F2}ms exceeds 100ms threshold");
    }

    [Fact]
    public async Task RedirectLookup_ConcurrentRequests_P99Under100ms()
    {
        // Warmup — sequential to prime JIT
        for (var i = 0; i < 50; i++)
        {
            var (prefix, code) = _testLinks[i];
            await _repository.GetByPrefixAndCodeAsync(new DomainPrefix(prefix), new ShortCode(code));
        }

        // Warmup — concurrent to prime connection pool
        var warmupTasks = Enumerable.Range(50, 200).Select(i =>
        {
            var (prefix, code) = _testLinks[i];
            return _repository.GetByPrefixAndCodeAsync(new DomainPrefix(prefix), new ShortCode(code));
        });
        await Task.WhenAll(warmupTasks);

        // Measure — 1,000 lookups with concurrency of 50 (realistic connection pool)
        var random = new Random(42);
        var latencies = new System.Collections.Concurrent.ConcurrentBag<double>();
        var semaphore = new SemaphoreSlim(50);

        var tasks = Enumerable.Range(0, 1000).Select(_ =>
        {
            var (prefix, code) = _testLinks[random.Next(_testLinks.Count)];
            return Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var sw = Stopwatch.StartNew();
                    var link = await _repository.GetByPrefixAndCodeAsync(new DomainPrefix(prefix), new ShortCode(code));
                    sw.Stop();

                    Assert.NotNull(link);
                    latencies.Add(sw.Elapsed.TotalMilliseconds);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }).ToArray();

        await Task.WhenAll(tasks);

        var sorted = latencies.OrderBy(l => l).ToList();
        var p50 = sorted[(int)(sorted.Count * 0.50)];
        var p95 = sorted[(int)(sorted.Count * 0.95)];
        var p99 = sorted[(int)(sorted.Count * 0.99)];
        var max = sorted[^1];

        Console.WriteLine($"Concurrent redirect latency (ms) — P50: {p50:F2}, P95: {p95:F2}, P99: {p99:F2}, Max: {max:F2}");

        Assert.True(p99 < 100, $"P99 latency {p99:F2}ms exceeds 100ms threshold");
    }

    private async Task SeedLinksAsync()
    {
        var links = _context.GetCollection<LinkDocument>("links");
        var batch = new List<LinkDocument>(1000);
        var counter = 0;

        for (var i = 0; i < 10_000; i++)
        {
            // 2-3 char lowercase prefix: "aa"-"dv" (50 unique prefixes)
            var prefixNum = i % 50;
            var prefix = $"{(char)('a' + prefixNum / 26)}{(char)('a' + prefixNum % 26)}";

            // 6 char alphanumeric code: "c00000"-"c09999" (unique per link)
            var code = $"c{i:D5}";

            _testLinks.Add((prefix, code));

            batch.Add(new LinkDocument
            {
                Id = Guid.NewGuid(),
                ShortCode = code,
                DomainPrefix = prefix,
                DestinationUrl = $"https://example.com/page/{i}",
                Status = "Active",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = null,
                CreatedBy = "perf-test",
                Metadata = null,
                Version = 1
            });

            if (batch.Count >= 1000)
            {
                await links.InsertManyAsync(batch);
                counter += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await links.InsertManyAsync(batch);
            counter += batch.Count;
        }

        Assert.Equal(10_000, counter);
    }
}
