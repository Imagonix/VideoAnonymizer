using System.Net.Mail;

namespace VideoAnonymizer.Contracts
{
    public record AnalyzeVideo(Guid videoId,string path, DateTimeOffset addedAt, int captureIntervalMs);
}
