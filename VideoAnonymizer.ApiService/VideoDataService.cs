using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService
{
    public class VideoDataService(IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    {
        public async Task<List<AnalyzedFrameDto>> GetAnalyzedVideo(Guid videoId)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = db.Videos.Where(x => x.Id.Equals(videoId)).Include(x => x.AnalyzedFrames).ThenInclude(x => x.DetectedObjects).SingleOrDefault(); 
            if (video == null)
            {
                throw new NotFoundException();
            }
            var dtos = video.AnalyzedFrames.Select(x => Mapper.ToDto(x)).ToList();
            return dtos;
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

        public async Task<Video> UpdateFramesAndObjects(Guid videoId, List<AnalyzedFrameDto> frames)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var existingVideo = db.Videos.Where(x => x.Id.Equals(videoId)).Include(x => x.AnalyzedFrames).ThenInclude(x => x.DetectedObjects).SingleOrDefault();
            if (existingVideo == null)
            {
                throw new NotFoundException();
            }
            db.RemoveRange(existingVideo.AnalyzedFrames.SelectMany(x => x.DetectedObjects));
            db.RemoveRange(existingVideo.AnalyzedFrames);
            var framesEntities = Mapper.ToEntities(frames);
            await db.AddRangeAsync(framesEntities);
            //var detectedObjectEntities = Mapper.ToEntities(frames.SelectMany(x => x.DetectedObjects));
            //await db.AddRangeAsync(detectedObjectEntities);
            await db.SaveChangesAsync();
            return existingVideo;
        }

        public async Task<string> LoadAnonomyzedVideoPath(Guid id)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = db.Videos.Where(x => x.Id.Equals(id)).SingleOrDefault();
            if (video == null || string.IsNullOrWhiteSpace(video.AnonomizedPath) || !File.Exists(video.AnonomizedPath))
            {
                throw new NotFoundException();
            }
            return video.AnonomizedPath;
        }
    }
}
