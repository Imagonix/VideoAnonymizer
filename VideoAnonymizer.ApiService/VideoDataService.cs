using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService
{
    public class VideoDataService(IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    {
        public async Task<Video> GetAnalyzedVideo(Guid videoId)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = db.Videos.Where(x => x.Id.Equals(videoId)).Include(x => x.AnalyzedFrames).ThenInclude(x => x.DetectedObjects).SingleOrDefault();
            if (video == null)
            {
                throw new NotFoundException();
            }
            return video;
        }

        public async Task<(Guid videoId, string fullPath)> SaveVideoFileAndCreateDbEntry(IFormFile uploadedVideo, string extension, string contentRootPath, CancellationToken cancellationToken)
        {
            var video = new Video();

            var uploadsRoot = Path.Combine(contentRootPath, "App_Data", "Uploads");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{video.Id}{extension}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);
            video.SourcePath = fullPath;

            await using (var fileStream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 64,
                useAsync: true))
            {
                await uploadedVideo.CopyToAsync(fileStream, cancellationToken);
            }

            using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            await db.Videos.AddAsync(video, cancellationToken);
            await db.SaveChangesAsync();
            return (video.Id, fullPath);
        }

        public async Task<Video> UpdateFramesAndObjects(Guid videoId, List<AnalyzedFrame> frames)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var existingVideo = db.Videos.Where(x => x.Id.Equals(videoId)).Include(x => x.AnalyzedFrames).ThenInclude(x => x.DetectedObjects).SingleOrDefault();
            if (existingVideo == null)
            {
                throw new NotFoundException();
            }
            db.RemoveRange(existingVideo.AnalyzedFrames.SelectMany(x => x.DetectedObjects));
            db.RemoveRange(existingVideo.AnalyzedFrames);
            await db.AddRangeAsync(frames);
            await db.AddRangeAsync(frames.SelectMany(x => x.DetectedObjects));
            await db.SaveChangesAsync();
            return existingVideo;
        }

        public async Task<Video> LoadVideo(Guid id)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = db.Videos.Where(x => x.Id.Equals(id)).SingleOrDefault();
            if (video == null)
            {
                throw new NotFoundException();
            }
            return video;
        }
    }
}
