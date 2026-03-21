using System.Net.Mail;

namespace VideoAnonymizer.Contracts
{
    public record AnonomyzeVideo(Guid jobId, Guid videoId, DateTimeOffset AddedAt);
}
