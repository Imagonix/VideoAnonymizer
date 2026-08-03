using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.DataServices
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
            var dtos = video.AnalyzedFrames
                .OrderBy(x => x.FrameIndex)
                .Select(x => Mapper.ToDto(x))
                .ToList();
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
            var rows = await db.Videos
                .OrderByDescending(v => v.Id)
                .Select(v => new
                {
                    v.Id,
                    OriginalFileName = v.OriginalFileName ?? "Unknown",
                    v.BlurSizePercent,
                    v.TimeBufferMs,
                    HasAnalysis = v.AnalyzedFrames.Any(),
                    HasAnonymizedOutput = v.AnonomizedPath != null && v.AnonomizedPath != ""
                })
                .ToListAsync();

            return rows.Select(v => new VideoDto
            {
                Id = v.Id,
                OriginalFileName = v.OriginalFileName,
                BlurSizePercent = v.BlurSizePercent,
                TimeBufferMs = v.TimeBufferMs,
                HasAnalysis = v.HasAnalysis,
                HasAnonymizedOutput = v.HasAnonymizedOutput,
                Status = VideoListStatuses.FromFlags(v.HasAnalysis, v.HasAnonymizedOutput)
            }).ToList();
        }

        /// <summary>
        /// Removes the working copy: analysis/editor rows, the video record, and managed media files.
        /// Paths are resolved and validated against configured storage roots before file deletion.
        /// </summary>
        public async Task<DeleteVideoResultDto> DeleteWorkingCopyAsync(Guid videoId, string contentRootPath)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = await db.Videos
                .Include(v => v.AnalyzedFrames)
                .ThenInclude(f => f.DetectedObjects)
                .FirstOrDefaultAsync(v => v.Id == videoId);

            if (video is null)
                throw new NotFoundException();

            var sourcePath = video.SourcePath;
            var anonymizedPath = video.AnonomizedPath;

            var editorActions = await db.EditorActions
                .Where(a => a.VideoId == videoId)
                .ToListAsync();

            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (editorActions.Count > 0)
                    db.EditorActions.RemoveRange(editorActions);

                db.Videos.Remove(video);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var result = new DeleteVideoResultDto
            {
                VideoId = videoId,
                DatabaseDeleted = true
            };

            result.SourceFileDeleted = TryDeleteManagedFile(
                sourcePath,
                videoId,
                contentRootPath,
                result.Warnings,
                "source");

            result.AnonymizedFileDeleted = string.IsNullOrWhiteSpace(anonymizedPath)
                || TryDeleteManagedFile(
                    anonymizedPath,
                    videoId,
                    contentRootPath,
                    result.Warnings,
                    "anonymized");

            return result;
        }

        /// <summary>
        /// Returns true when the path is an absolute managed media path for this video
        /// under App_Data/Uploads relative to contentRoot or an equivalent hosted volume layout.
        /// </summary>
        internal static bool TryResolveManagedMediaPath(
            string path,
            Guid videoId,
            string contentRootPath,
            out string fullPath,
            out string rejectionReason)
        {
            fullPath = string.Empty;
            rejectionReason = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                rejectionReason = "path is empty";
                return false;
            }

            string resolved;
            try
            {
                resolved = Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                rejectionReason = $"path could not be resolved ({ex.Message})";
                return false;
            }

            if (!Path.IsPathRooted(resolved))
            {
                rejectionReason = "path is not absolute";
                return false;
            }

            var fileName = Path.GetFileName(resolved);
            var videoIdText = videoId.ToString();
            if (!fileName.StartsWith(videoIdText, StringComparison.OrdinalIgnoreCase))
            {
                rejectionReason = "file name does not match the video id";
                return false;
            }

            var directory = Path.GetDirectoryName(resolved);
            if (string.IsNullOrWhiteSpace(directory))
            {
                rejectionReason = "path has no directory";
                return false;
            }

            string resolvedDirectory;
            try
            {
                resolvedDirectory = Path.GetFullPath(directory);
            }
            catch (Exception ex)
            {
                rejectionReason = $"directory could not be resolved ({ex.Message})";
                return false;
            }

            var standaloneUploadsRoot = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "Uploads"));
            var uploadsSuffix = Path.DirectorySeparatorChar + Path.Combine("App_Data", "Uploads");
            var altUploadsSuffix = Path.AltDirectorySeparatorChar + "App_Data" + Path.AltDirectorySeparatorChar + "Uploads";

            var underStandaloneRoot = resolvedDirectory.StartsWith(standaloneUploadsRoot, StringComparison.OrdinalIgnoreCase);
            var underHostedUploadsLayout =
                resolvedDirectory.EndsWith(uploadsSuffix, StringComparison.OrdinalIgnoreCase)
                || resolvedDirectory.EndsWith(altUploadsSuffix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(resolvedDirectory, standaloneUploadsRoot, StringComparison.OrdinalIgnoreCase);

            if (!underStandaloneRoot && !underHostedUploadsLayout)
            {
                rejectionReason = "path is outside configured working-copy storage";
                return false;
            }

            fullPath = resolved;
            return true;
        }

        private static bool TryDeleteManagedFile(
            string? path,
            Guid videoId,
            string contentRootPath,
            List<string> warnings,
            string label)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (!TryResolveManagedMediaPath(path, videoId, contentRootPath, out var fullPath, out var reason))
            {
                warnings.Add($"Skipped {label} file: {reason}.");
                return false;
            }

            try
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not delete {label} file '{fullPath}': {ex.Message}");
                return false;
            }
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

        public async Task UpdateFramesAndObjects(Guid videoId, AnonymizeVideoRequestDto request)
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
