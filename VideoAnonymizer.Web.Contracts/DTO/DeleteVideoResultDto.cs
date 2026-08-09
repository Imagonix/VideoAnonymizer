namespace VideoAnonymizer.Web.Shared.DTO;

/// <summary>
/// Result of removing a working copy (database graph + managed source/anonymized files).
/// </summary>
public sealed class DeleteVideoResultDto
{
    public Guid VideoId { get; set; }
    public bool DatabaseDeleted { get; set; }
    public bool SourceFileDeleted { get; set; }
    public bool AnonymizedFileDeleted { get; set; }
    public List<string> Warnings { get; set; } = [];
}
