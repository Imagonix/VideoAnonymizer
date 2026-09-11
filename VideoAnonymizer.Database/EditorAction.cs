namespace VideoAnonymizer.Database;

public class EditorAction : EntityBase
{
    public Guid VideoId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Undone { get; set; }
    public string Data { get; set; } = string.Empty;
}
