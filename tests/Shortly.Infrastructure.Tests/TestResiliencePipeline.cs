using Polly;
using Polly.Registry;
using Shortly.Infrastructure.Resilience;

namespace Shortly.Infrastructure.Tests;

/// <summary>
/// Provides a no-op resilience pipeline provider for integration tests
/// where resilience policies are not under test.
/// </summary>
public static class TestResiliencePipeline
{
    private static readonly ResiliencePipelineRegistry<string> Registry = CreateRegistry();

    public static ResiliencePipelineProvider<string> Provider => Registry;

    private static ResiliencePipelineRegistry<string> CreateRegistry()
    {
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(ResilienceKeys.MongoDb, (builder, _) => { });
        registry.TryAddBuilder(ResilienceKeys.ServiceBus, (builder, _) => { });
        return registry;
    }
}
