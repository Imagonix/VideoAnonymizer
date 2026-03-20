using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService
{
    public class VideoDataService(IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    {
        internal object GetAnalyzedVideo()
        {
            throw new NotImplementedException();
        }
    }
}
