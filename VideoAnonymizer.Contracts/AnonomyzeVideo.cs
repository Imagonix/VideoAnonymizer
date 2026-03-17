using System.Net.Mail;

namespace VideoAnonymizer.Contracts
{
    public record AnonomyzeVideo(Guid jobId, string Path, DateTimeOffset AddedAt);
}
