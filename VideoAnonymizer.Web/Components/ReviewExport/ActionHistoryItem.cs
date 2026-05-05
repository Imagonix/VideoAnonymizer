namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class ActionHistoryItem(string description, DateTime timestamp)
{
    public string Description { get; } = description;
    public ActionStatus Status { get; set; } = ActionStatus.Pending;
    public DateTime Timestamp { get; } = timestamp;
    public bool Undone { get; set; }
}
