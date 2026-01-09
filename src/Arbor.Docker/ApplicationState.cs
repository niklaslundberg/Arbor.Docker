namespace Arbor.Docker;

internal enum ApplicationState{
    NotStarted,
    Starting,
    Started,
    Stopping,
    Stopped,
    Failed
}