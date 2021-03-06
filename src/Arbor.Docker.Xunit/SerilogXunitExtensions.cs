﻿using Serilog;
using Xunit.Abstractions;

namespace Arbor.Docker.Xunit
{
    public static class SerilogXunitExtensions
    {
        public static ILogger ToLogger(this ITestOutputHelper testOutputHelper) =>
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(testOutputHelper)
                .CreateLogger();
    }
}