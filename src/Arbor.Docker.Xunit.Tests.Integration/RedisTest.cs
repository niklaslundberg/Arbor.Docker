using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arbor.Docker.Xunit.Tests.Integration;

public class RedisTest(ITestOutputHelper outputHelper) : DockerTest(outputHelper.ToLogger())
{
    protected override async IAsyncEnumerable<ContainerSettings> AddContainersAsync()
    {
        var portMappings = new[] {PortMapping.MapSinglePort(36379, 6379)};
        yield return new ContainerSettings(
            "redis",
            "redistest",
            portMappings,
            entryPoint: ["redis-server", "--appendonly yes"]
        );
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task SetAndGetFromDistributedCache()
    {
        var redisCacheOptions = new RedisCacheOptions {Configuration = "localhost:36379"};
        IDistributedCache distributedCache =
            new RedisCache(new OptionsWrapper<RedisCacheOptions>(redisCacheOptions));

        await distributedCache.SetStringAsync("test", "hello world", token: TestContext.Current.CancellationToken);

        string? cachedValue = await distributedCache.GetStringAsync("test", token: TestContext.Current.CancellationToken);

        Assert.Equal("hello world", cachedValue);
    }
}