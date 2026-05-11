using Microsoft.Extensions.Options;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.CodeGeneration;
using Shortly.Infrastructure.Configuration;
using Testcontainers.MongoDb;

namespace Shortly.Infrastructure.Tests.Persistence;

public sealed class RangeBasedCodeGeneratorIntegrationTests : IAsyncLifetime
{
    private MongoDbContainer _container = null!;
    private RangeBasedCodeGenerator _generator = null!;

    public async Task InitializeAsync()
    {
        _container = new MongoDbBuilder("mongo:7")
            .Build();

        await _container.StartAsync();

        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _container.GetConnectionString(),
            DatabaseName = "shortly_codegen_test"
        });

        _generator = new RangeBasedCodeGenerator(settings);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    // --- Basic Generation ---

    [Fact]
    public async Task GenerateAsync_ReturnsValidShortCode()
    {
        var prefix = new DomainPrefix("ab");

        var code = await _generator.GenerateAsync(prefix);

        Assert.NotNull(code);
        Assert.InRange(code.Value.Length, 5, 8);
    }

    [Fact]
    public async Task GenerateAsync_ConsecutiveCalls_ReturnUniqueCodesForSamePrefix()
    {
        var prefix = new DomainPrefix("cd");
        var codes = new HashSet<string>();

        for (var i = 0; i < 100; i++)
        {
            var code = await _generator.GenerateAsync(prefix);
            Assert.True(codes.Add(code.Value), $"Duplicate code generated: {code.Value}");
        }

        Assert.Equal(100, codes.Count);
    }

    [Fact]
    public async Task GenerateAsync_DifferentPrefixes_IndependentCounters()
    {
        var prefix1 = new DomainPrefix("ef");
        var prefix2 = new DomainPrefix("gh");

        var code1 = await _generator.GenerateAsync(prefix1);
        var code2 = await _generator.GenerateAsync(prefix2);

        // Both should be valid but can be the same value since counters are independent
        Assert.NotNull(code1);
        Assert.NotNull(code2);
        Assert.InRange(code1.Value.Length, 5, 8);
        Assert.InRange(code2.Value.Length, 5, 8);
    }

    // --- Range exhaustion and re-claim ---

    [Fact]
    public async Task GenerateAsync_MoreThan1000Codes_ClaimsNewRange()
    {
        var prefix = new DomainPrefix("ij");
        var codes = new HashSet<string>();

        // Generate 1050 codes to force at least one range re-claim (range size = 1000)
        for (var i = 0; i < 1050; i++)
        {
            var code = await _generator.GenerateAsync(prefix);
            Assert.True(codes.Add(code.Value), $"Duplicate at iteration {i}: {code.Value}");
        }

        Assert.Equal(1050, codes.Count);
    }

    // --- Concurrency ---

    [Fact]
    public async Task GenerateAsync_ConcurrentCalls_NoDuplicates()
    {
        var prefix = new DomainPrefix("kl");
        var codes = new System.Collections.Concurrent.ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, 200).Select(_ => Task.Run(async () =>
        {
            var code = await _generator.GenerateAsync(prefix);
            codes.Add(code.Value);
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(200, codes.Count);
        Assert.Equal(200, codes.Distinct().Count());
    }

    // --- Multiple instances sharing counter ---

    [Fact]
    public async Task GenerateAsync_1000ConcurrentRequests_AllUnique()
    {
        var prefix = new DomainPrefix("op");
        var codes = new System.Collections.Concurrent.ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, 1000).Select(_ => Task.Run(async () =>
        {
            var code = await _generator.GenerateAsync(prefix);
            codes.Add(code.Value);
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(1000, codes.Count);
        Assert.Equal(1000, codes.Distinct().Count());
    }

    [Fact]
    public async Task GenerateAsync_1000ConcurrentRequests_MultipleInstances_AllUnique()
    {
        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _container.GetConnectionString(),
            DatabaseName = "shortly_codegen_test"
        });

        var generator1 = new RangeBasedCodeGenerator(settings);
        var generator2 = new RangeBasedCodeGenerator(settings);
        var generator3 = new RangeBasedCodeGenerator(settings);

        var generators = new[] { generator1, generator2, generator3 };
        var prefix = new DomainPrefix("qr");
        var codes = new System.Collections.Concurrent.ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, 1000).Select(i => Task.Run(async () =>
        {
            var gen = generators[i % 3];
            var code = await gen.GenerateAsync(prefix);
            codes.Add(code.Value);
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(1000, codes.Count);
        Assert.Equal(1000, codes.Distinct().Count());
    }

    [Fact]
    public async Task GenerateAsync_TwoInstances_NoDuplicates()
    {
        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _container.GetConnectionString(),
            DatabaseName = "shortly_codegen_test"
        });

        var generator1 = new RangeBasedCodeGenerator(settings);
        var generator2 = new RangeBasedCodeGenerator(settings);

        var prefix = new DomainPrefix("mn");
        var codes = new HashSet<string>();

        for (var i = 0; i < 50; i++)
        {
            var code1 = await generator1.GenerateAsync(prefix);
            var code2 = await generator2.GenerateAsync(prefix);
            Assert.True(codes.Add(code1.Value), $"Duplicate from gen1: {code1.Value}");
            Assert.True(codes.Add(code2.Value), $"Duplicate from gen2: {code2.Value}");
        }

        Assert.Equal(100, codes.Count);
    }
}
