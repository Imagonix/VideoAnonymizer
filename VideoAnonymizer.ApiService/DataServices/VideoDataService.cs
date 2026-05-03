using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.DataServices
{
    public class VideoDataService(IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    {
        public async Task<DetectedObjectDto> AddDetectedObject(Guid videoId, DetectedObjectDto dto)
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var frame = await db.AnalyzedFrames
                .FirstOrDefaultAsync(f => f.Id == dto.AnalyzedFrameId && f.VideoId == videoId);
            if (frame == null)
                throw new NotFoundException();

            var entity = Mapper.ToEntity(dto);
            db.DetectedObjects.Add(entity);
            await db.SaveChangesAsync();

            return Mapper.ToDto(entity);
        }

        public async Task<DetectedObjectDto> UpdateDetectedObject(Guid videoId, DetectedObjectDto dto)
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var entity = await db.DetectedObjects
                .Include(o => o.AnalyzedFrame)
                .FirstOrDefaultAsync(o => o.Id == dto.Id && o.AnalyzedFrame.VideoId == videoId);
            if (entity == null)
                throw new NotFoundException();

            Mapper.UpdateEntity(dto, entity);
            await db.SaveChangesAsync();

            return Mapper.ToDto(entity);
        }

        public async Task BulkUpdateDetectedObjects(Guid videoId, IReadOnlyList<DetectedObjectDto> dtos)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            var objectIds = dtos.Select(d => d.Id).ToHashSet();

            var existing = await db.DetectedObjects
                .Include(o => o.AnalyzedFrame)
                .Where(o => objectIds.Contains(o.Id) && o.AnalyzedFrame.VideoId == videoId)
                .ToListAsync();

            if (existing.Count != dtos.Count)
                throw new NotFoundException();

            var dtoMap = dtos.ToDictionary(d => d.Id);
            foreach (var entity in existing)
            {
                Mapper.UpdateEntity(dtoMap[entity.Id], entity);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

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

        public async Task<string> LoadOriginalVideoPath(Guid videoId)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = await db.Videos
                .FirstOrDefaultAsync(v => v.Id == videoId);

            if (video == null)
                throw new NotFoundException();

            return video.SourcePath;
        }

        public async Task<List<VideoDto>> GetVideos()
        {
            using var db = await dbFactory.CreateDbContextAsync();
            return await db.Videos
                .OrderByDescending(v => v.Id)
                .Select(v => new VideoDto
                {
                    Id = v.Id,
                    OriginalFileName = v.OriginalFileName ?? "Unknown",
                    BlurSizePercent = v.BlurSizePercent,
                    TimeBufferMs = v.TimeBufferMs
                })
                .ToListAsync();
        }

        public async Task UpdateVideoSettings(Guid videoId, int blurSizePercent, int timeBufferMs)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null)
                throw new NotFoundException();

            video.BlurSizePercent = blurSizePercent;
            video.TimeBufferMs = timeBufferMs;
            await db.SaveChangesAsync();
        }

        public async Task<(Guid videoId, string fullPath)> SaveVideoFileAndCreateDbEntry(IFormFile uploadedVideo, string originalFileName, string extension, string contentRootPath, CancellationToken cancellationToken)
        {
            var video = new Video { OriginalFileName = originalFileName, BlurSizePercent = 120, TimeBufferMs = 300 };

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

        public async Task<Video> UpdateFramesAndObjects(Guid videoId, AnonymizeVideoRequestDto request)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var existingVideo = db.Videos.Where(x => x.Id.Equals(videoId)).Include(x => x.AnalyzedFrames).ThenInclude(x => x.DetectedObjects).SingleOrDefault();
            if (existingVideo == null)
            {
                throw new NotFoundException();
            }
            if (request.Settings is not null)
            {
                existingVideo.BlurSizePercent = request.Settings.BlurSizePercent;
                existingVideo.TimeBufferMs = request.Settings.TimeBufferMs;
            } else
            {
                existingVideo.BlurSizePercent = 130;
                existingVideo.TimeBufferMs = 100;
            }
            db.RemoveRange(existingVideo.AnalyzedFrames.SelectMany(x => x.DetectedObjects));
            db.RemoveRange(existingVideo.AnalyzedFrames);
            var framesEntities = Mapper.ToEntities(request.Frames);
            await db.AddRangeAsync(framesEntities);
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
