namespace Arbor.Docker;

internal enum PublishMode
{
    AwaitedSequential,
    AwaitedParallel,
    FireAndForgetSequential,
    FireAndForgetParallel
}