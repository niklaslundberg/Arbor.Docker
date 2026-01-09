using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Serilog;

namespace Arbor.Docker;

public static class DockerHelper
{
    public static async Task<ExitCode> RunDockerCommandsAsync(
        IEnumerable<string> args,
        ILogger logger,
        string? dockerExePath = null,
        bool logAsDebug = false,
        CancellationToken token = default)
    {
        FileInfo exePath = GetDockerExePath(dockerExePath);

        Process[] dockerProcesses = Process.GetProcesses().Where(process => IsDockerProcess(process, exePath)).ToArray();

        if (dockerProcesses.Length == 0)
        {
            var start = await ProcessRunner.ExecuteProcessAsync(
                exePath.FullName,
                arguments: ["desktop", "start"],
                standardOutLog: (message, _) => logger.Information("{Message}", message),
                cancellationToken: token);

            if (!start.IsSuccess)
            {
                throw new InvalidOperationException("Could not start Docker");
            }
        }

        void LogDebug(string message, string? _) => logger.Debug("{Message}", message);

        void LogError(string message, string? _) => logger.Error("{Message}", message);

        void LogInformation(string message, string? _) => logger.Information("{Message}", message);

        var exitCode = await ProcessRunner.ExecuteProcessAsync(
            exePath.FullName,
            args,
            logAsDebug
                ? LogDebug
                : (CategoryLog)LogInformation,
            logAsDebug
                ? (CategoryLog)LogDebug
                : LogError,
            toolAction: LogDebug,
            verboseAction: (message, _) => logger.Verbose("{Message}", message),
            debugAction: LogDebug,
            formatArgs: false,
            cancellationToken: token);

        if (!exitCode.IsSuccess)
        {
            logger.Error("Docker failed");
        }

        return exitCode;
    }

    private static FileInfo GetDockerExePath(string? dockerExePath)
    {
        var candidatePaths = new List<string>(2)
        {
            @"C:\Program Files\Docker\docker.exe",
            @"C:\Program Files\Docker\Docker\Resources\bin\docker.exe",
            @"C:\ProgramData\DockerDesktop\version-bin\docker.exe"
        };

        dockerExePath ??= candidatePaths.FirstOrDefault(File.Exists);

        if (dockerExePath is null)
        {
            throw new InvalidOperationException("Could not find docker.exe");
        }

        var exePath = new FileInfo(dockerExePath);

        if (!exePath.Exists)
        {
            throw new InvalidOperationException($"The docker exe file '{exePath.FullName}' does not exist");
        }

        return exePath;
    }

    private static bool IsDockerProcess(Process process, FileInfo dockerFilePath)
    {
        return false;// TODO
        try
        {
            if (process.MainModule is { } module && module.FileName.Equals(dockerFilePath.Name))
            {
                return true;
            }

        }
        catch (Exception ex)
        {

        }

        return false;
    }
}