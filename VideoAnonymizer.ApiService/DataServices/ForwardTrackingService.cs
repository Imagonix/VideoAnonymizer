using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.DataServices;

public sealed class ForwardTrackingService(
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    global::VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient objectDetectionClient)
{
    private const double ConflictIouThreshold = 0.30;

    public async Task<TrackForwardResponseDto> TrackForwardAsync(
        Guid videoId,
        TrackForwardRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidateOptions(request);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var video = await db.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video is null)
            throw new NotFoundException();

        if (!File.Exists(video.SourcePath))
            throw new FileNotFoundException("Video file not found.", video.SourcePath);

        var frames = await db.AnalyzedFrames
            .Include(frame => frame.DetectedObjects)
            .Where(frame => frame.VideoId == videoId)
            .OrderBy(frame => frame.FrameIndex)
            .ToListAsync(cancellationToken);

        if (frames.Count == 0)
            throw new ArgumentException("The video has no analyzed frames.");

        var seed = ResolveSeed(frames, request);
        var updatedObjects = new List<DetectedObjectDto>();
        var trackId = seed.Object.TrackId ?? NextTrackId(frames);
        var seedChanged = seed.WasCreated || seed.Object.TrackId != trackId;
        if (seed.Object.TrackId != trackId)
        {
            seed.Object.TrackId = trackId;
        }

        if (seed.WasCreated)
        {
            db.DetectedObjects.Add(seed.Object);
        }

        if (seedChanged)
        {
            updatedObjects.Add(seed.Object.ToDto());
        }

        var persistEveryMs = request.PersistEveryMs ?? InferPersistEveryMs(frames);
        var maxTrackDurationMs = Math.Max(1, request.MaxTrackDurationMs);
        var maxTrackTimeSeconds = seed.Frame.TimeSeconds + maxTrackDurationMs / 1000.0;
        var persistFrameIndexes = frames
            .Where(frame =>
                frame.FrameIndex > seed.Frame.FrameIndex
                && frame.TimeSeconds <= maxTrackTimeSeconds)
            .Select(frame => frame.FrameIndex)
            .ToList();

        if (persistFrameIndexes.Count == 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            return new TrackForwardResponseDto
            {
                TrackId = trackId,
                StoppedReason = "no_future_frames",
                UpdatedObjects = updatedObjects,
                UpdatedFrames = await LoadUpdatedFramesAsync(db, videoId, [seed.Frame.Id], cancellationToken)
            };
        }

        var pythonResponse = await objectDetectionClient.TrackForwardAsync(
            new TrackForwardPythonRequest
            {
                VideoPath = video.SourcePath,
                SeedFrameIndex = seed.Frame.FrameIndex,
                SeedTimeMs = ToMilliseconds(seed.Frame.TimeSeconds),
                BoundingBox = new TrackForwardPythonBoundingBox
                {
                    X = seed.Object.X,
                    Y = seed.Object.Y,
                    Width = seed.Object.Width,
                    Height = seed.Object.Height
                },
                ObjectClass = seed.Object.ClassName ?? "face",
                TrackId = trackId,
                PersistEveryMs = persistEveryMs,
                PersistFrameIndexes = persistFrameIndexes,
                MaxLostDurationMs = request.MaxLostDurationMs,
                RecoveryDetectorIntervalMs = request.RecoveryDetectorIntervalMs,
                TrackerType = request.TrackerType,
                SearchAreaExpansion = request.SearchAreaExpansion,
                MaxTrackDurationMs = maxTrackDurationMs
            },
            cancellationToken);

        var createdObjects = new List<DetectedObjectDto>();
        var updatedFrameIds = new HashSet<Guid> { seed.Frame.Id };
        var skippedConflicts = 0;

        foreach (var detection in pythonResponse.Detections.OrderBy(d => d.FrameIndex))
        {
            var targetFrame = frames.FirstOrDefault(frame => frame.FrameIndex == detection.FrameIndex);
            if (targetFrame is null)
                continue;

            if (HasSameTrackInFrame(targetFrame, trackId)
                || HasDifferentTrackConflict(targetFrame, detection, trackId))
            {
                skippedConflicts++;
                continue;
            }

            var entity = new DetectedObject
            {
                AnalyzedFrameId = targetFrame.Id,
                Confidence = detection.Confidence,
                ClassName = string.IsNullOrWhiteSpace(detection.ClassName) ? seed.Object.ClassName : detection.ClassName,
                BlurShape = detection.BlurShape ?? seed.Object.BlurShape,
                Selected = true,
                TrackId = trackId,
                X = detection.X,
                Y = detection.Y,
                Width = detection.Width,
                Height = detection.Height
            };

            db.DetectedObjects.Add(entity);
            targetFrame.DetectedObjects.Add(entity);
            createdObjects.Add(entity.ToDto());
            updatedFrameIds.Add(targetFrame.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        return new TrackForwardResponseDto
        {
            TrackId = trackId,
            CreatedDetections = createdObjects.Count,
            SkippedConflicts = skippedConflicts,
            ReacquiredCount = pythonResponse.ReacquiredCount,
            StoppedReason = pythonResponse.StoppedReason,
            Gaps = pythonResponse.Gaps
                .Select(gap => new TrackForwardGapDto
                {
                    StartTimeMs = gap.StartTimeMs,
                    EndTimeMs = gap.EndTimeMs
                })
                .ToList(),
            UpdatedObjects = updatedObjects,
            CreatedObjects = createdObjects,
            UpdatedFrames = await LoadUpdatedFramesAsync(db, videoId, updatedFrameIds, cancellationToken)
        };
    }

    private static void ValidateOptions(TrackForwardRequestDto request)
    {
        if (!string.Equals(request.ConflictMode, "skip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only conflictMode 'skip' is supported.");

        if (request.PersistEveryMs is <= 0)
            throw new ArgumentException("persistEveryMs must be greater than zero.");

        if (request.MaxLostDurationMs <= 0)
            throw new ArgumentException("maxLostDurationMs must be greater than zero.");

        if (request.RecoveryDetectorIntervalMs <= 0)
            throw new ArgumentException("recoveryDetectorIntervalMs must be greater than zero.");

        if (request.SearchAreaExpansion < 1)
            throw new ArgumentException("searchAreaExpansion must be at least 1.0.");

        if (request.MaxTrackDurationMs <= 0)
            throw new ArgumentException("maxTrackDurationMs must be greater than zero.");
    }

    private static SeedObject ResolveSeed(IReadOnlyList<AnalyzedFrame> frames, TrackForwardRequestDto request)
    {
        if (request.SeedDetectionId.HasValue)
        {
            var seedObject = frames
                .SelectMany(frame => frame.DetectedObjects)
                .FirstOrDefault(obj => obj.Id == request.SeedDetectionId.Value);
            if (seedObject is null)
                throw new NotFoundException();

            var seedFrame = frames.First(frame => frame.Id == seedObject.AnalyzedFrameId);
            ValidateBox(seedObject.X, seedObject.Y, seedObject.Width, seedObject.Height);
            return new SeedObject(seedFrame, seedObject, WasCreated: false);
        }

        if (request.BoundingBox is null)
            throw new ArgumentException("A boundingBox is required when seedDetectionId is not provided.");

        ValidateBox(
            request.BoundingBox.X,
            request.BoundingBox.Y,
            request.BoundingBox.Width,
            request.BoundingBox.Height);

        if (string.IsNullOrWhiteSpace(request.ObjectClass))
            throw new ArgumentException("objectClass is required when seedDetectionId is not provided.");

        var explicitSeedFrame = ResolveExplicitSeedFrame(frames, request);
        var explicitSeedObject = new DetectedObject
        {
            AnalyzedFrameId = explicitSeedFrame.Id,
            Confidence = 1,
            ClassName = request.ObjectClass,
            Selected = true,
            TrackId = request.TrackId,
            X = request.BoundingBox.X,
            Y = request.BoundingBox.Y,
            Width = request.BoundingBox.Width,
            Height = request.BoundingBox.Height
        };

        explicitSeedFrame.DetectedObjects.Add(explicitSeedObject);
        return new SeedObject(explicitSeedFrame, explicitSeedObject, WasCreated: true);
    }

    private static AnalyzedFrame ResolveExplicitSeedFrame(IReadOnlyList<AnalyzedFrame> frames, TrackForwardRequestDto request)
    {
        if (request.SeedFrameIndex.HasValue)
        {
            var frame = frames.FirstOrDefault(frame => frame.FrameIndex == request.SeedFrameIndex.Value);
            return frame ?? throw new NotFoundException();
        }

        var seedSeconds = request.SeedTimeMs / 1000.0;
        return frames
            .OrderBy(frame => Math.Abs(frame.TimeSeconds - seedSeconds))
            .First();
    }

    private static void ValidateBox(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Bounding box width and height must be greater than zero.");
    }

    private static int NextTrackId(IEnumerable<AnalyzedFrame> frames) =>
        frames
            .SelectMany(frame => frame.DetectedObjects)
            .Select(obj => obj.TrackId ?? 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

    private static int InferPersistEveryMs(IReadOnlyList<AnalyzedFrame> frames)
    {
        var intervals = frames
            .OrderBy(frame => frame.TimeSeconds)
            .Zip(frames.OrderBy(frame => frame.TimeSeconds).Skip(1))
            .Select(pair => (pair.Second.TimeSeconds - pair.First.TimeSeconds) * 1000.0)
            .Where(ms => ms > 0)
            .OrderBy(ms => ms)
            .ToList();

        if (intervals.Count == 0)
            return 100;

        return Math.Max(1, (int)Math.Round(intervals[intervals.Count / 2]));
    }

    private static bool HasSameTrackInFrame(AnalyzedFrame frame, int trackId) =>
        frame.DetectedObjects.Any(obj => obj.TrackId == trackId);

    private static bool HasDifferentTrackConflict(
        AnalyzedFrame frame,
        TrackForwardPythonDetectionResult detection,
        int trackId) =>
        frame.DetectedObjects.Any(existing =>
            existing.TrackId != trackId
            && CalculateIntersectionOverUnion(existing, detection) >= ConflictIouThreshold);

    private static double CalculateIntersectionOverUnion(
        DetectedObject existing,
        TrackForwardPythonDetectionResult detection)
    {
        var x1 = Math.Max(existing.X, detection.X);
        var y1 = Math.Max(existing.Y, detection.Y);
        var x2 = Math.Min(existing.X + existing.Width, detection.X + detection.Width);
        var y2 = Math.Min(existing.Y + existing.Height, detection.Y + detection.Height);

        var intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var existingArea = Math.Max(0, existing.Width) * Math.Max(0, existing.Height);
        var detectionArea = Math.Max(0, detection.Width) * Math.Max(0, detection.Height);
        var union = existingArea + detectionArea - intersection;

        return union <= 0 ? 0 : (double)intersection / union;
    }

    private static int ToMilliseconds(double timeSeconds) =>
        (int)Math.Round(timeSeconds * 1000.0);

    private static async Task<List<AnalyzedFrameDto>> LoadUpdatedFramesAsync(
        VideoAnonymizerDbContext db,
        Guid videoId,
        IEnumerable<Guid> frameIds,
        CancellationToken cancellationToken)
    {
        var ids = frameIds.ToHashSet();
        return await db.AnalyzedFrames
            .AsNoTracking()
            .Include(frame => frame.DetectedObjects)
            .Where(frame => frame.VideoId == videoId && ids.Contains(frame.Id))
            .OrderBy(frame => frame.FrameIndex)
            .Select(frame => frame.ToDto())
            .ToListAsync(cancellationToken);
    }

    private sealed record SeedObject(AnalyzedFrame Frame, DetectedObject Object, bool WasCreated);
}
