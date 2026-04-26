namespace VideoAnonymizer.Contracts;

public class VideoProcessingProgress
{
    public Guid JobId { get; set; }
    public Guid? VideoId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public int? ProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset AddedAt { get; set; }

    // for rabbitMq
    public VideoProcessingProgress() { }

    public VideoProcessingProgress(
        Guid jobId,
        Guid? videoId,
        string operation,
        int? progressPercent,
        string status,
        DateTimeOffset addedAt)
    {
        JobId = jobId;
        VideoId = videoId;
        Operation = operation;
        ProgressPercent = progressPercent;
        Status = status;
        AddedAt = addedAt;
    }
}
