using System.Net.Mail;

namespace VideoAnonymizer.Contracts
{
    public record AnonomyzedVideo(Guid jobId, DateTimeOffset AddedAt);
}
