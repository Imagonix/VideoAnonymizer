namespace VideoAnonymizer.Web.Shared.DTO;

public class EditorActionDto
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public string ActionType { get; set; } = "";
    public int SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Undone { get; set; }
    public string Data { get; set; } = "";
}
