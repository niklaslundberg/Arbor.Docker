using Serilog;
using Serilog.Events;
using Serilog.Sinks.XUnit;
using Xunit;

namespace Arbor.Docker.Xunit.Tests.Integration;

public static class SerilogXunitExtensions
{
    public static ILogger ToLogger(this ITestOutputHelper testOutputHelper, LogEventLevel minimumLevel = LogEventLevel.Verbose) =>
        new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.TestOutput(testOutputHelper)
            .CreateLogger();
}