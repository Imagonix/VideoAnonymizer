namespace VideoAnonymizer.Web.Modules.Actions;

public abstract record VideoEditorAction
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
