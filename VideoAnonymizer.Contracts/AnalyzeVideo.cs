namespace VideoAnonymizer.Contracts;

public class AnalyzeVideo
{
    public Guid VideoId { get; set; }
    public string Path { get; set; } = string.Empty;
    public DateTimeOffset AddedAt { get; set; }
    public int CaptureIntervalMs { get; set; }

    // for rabbitMq
    public AnalyzeVideo() { }

    public AnalyzeVideo(Guid videoId, string path, DateTimeOffset addedAt, int captureIntervalMs)
    {
        VideoId = videoId;
        Path = path;
        AddedAt = addedAt;
        CaptureIntervalMs = captureIntervalMs;
    }
}