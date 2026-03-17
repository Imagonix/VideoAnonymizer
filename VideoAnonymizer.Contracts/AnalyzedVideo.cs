using System.Net.Mail;

namespace VideoAnonymizer.Contracts
{
    public record AnalyzedVideo(Guid jobId, DateTimeOffset AddedAt);
}
