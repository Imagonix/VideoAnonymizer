using System.Net.Mail;

namespace VideoAnonymizer.Contracts
{
    public record AnalyzeVideo(Guid jobId, string Path, DateTimeOffset AddedAt);
}
