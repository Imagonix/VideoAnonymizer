using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

public sealed record TrackForwardResult(
    int TrackId,
    int CreatedDetections,
    int SkippedConflicts,
    int ReacquiredCount,
    string StoppedReason,
    IReadOnlyList<TrackForwardGap> Gaps,
    IReadOnlyList<Guid> CreatedObjectIds);

public sealed record TrackForwardGap(int StartTimeMs, int EndTimeMs);

public sealed class ForwardTrackingService(
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    global::VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient objectDetectionClient)
{
    private const double ConflictIouThreshold = 0.30;

    public async Task<TrackForwardResult> TrackForwardAsync(
        Guid videoId,
        TrackForwardJob job,
        CancellationToken cancellationToken)
    {
        ValidateOptions(job);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var video = await db.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video is null)
            throw new KeyNotFoundException($"Video {videoId} not found.");

        if (!File.Exists(video.SourcePath))
            throw new FileNotFoundException("Video file not found.", video.SourcePath);

        var frames = await db.AnalyzedFrames
            .Include(frame => frame.DetectedObjects)
            .Where(frame => frame.VideoId == videoId)
            .OrderBy(frame => frame.FrameIndex)
            .ToListAsync(cancellationToken);

        if (frames.Count == 0)
            throw new ArgumentException("The video has no analyzed frames.");

        var seed = ResolveSeed(frames, job);
        var trackId = seed.Object.TrackId ?? NextTrackId(frames);
        if (seed.Object.TrackId != trackId)
        {
            seed.Object.TrackId = trackId;
        }

        if (seed.WasCreated)
        {
            db.DetectedObjects.Add(seed.Object);
        }

        var persistEveryMs = job.PersistEveryMs ?? InferPersistEveryMs(frames);
        var maxTrackDurationMs = Math.Max(1, job.MaxTrackDurationMs);
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
            return new TrackForwardResult(trackId, 0, 0, 0, "no_future_frames", [], []);
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
                MaxLostDurationMs = job.MaxLostDurationMs,
                RecoveryDetectorIntervalMs = job.RecoveryDetectorIntervalMs,
                TrackerType = job.TrackerType,
                SearchAreaExpansion = job.SearchAreaExpansion,
                MaxTrackDurationMs = maxTrackDurationMs
            },
            cancellationToken);

        var createdCount = 0;
        var skippedConflicts = 0;
        var createdIds = new List<Guid>();

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
            createdIds.Add(entity.Id);
            createdCount++;
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        return new TrackForwardResult(
            trackId,
            createdCount,
            skippedConflicts,
            pythonResponse.ReacquiredCount,
            pythonResponse.StoppedReason,
            pythonResponse.Gaps
                .Select(gap => new TrackForwardGap(gap.StartTimeMs, gap.EndTimeMs))
                .ToList(),
            createdIds);
    }

    private static void ValidateOptions(TrackForwardJob job)
    {
        if (!string.Equals(job.ConflictMode, "skip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only conflictMode 'skip' is supported.");

        if (job.PersistEveryMs is <= 0)
            throw new ArgumentException("persistEveryMs must be greater than zero.");

        if (job.MaxLostDurationMs <= 0)
            throw new ArgumentException("maxLostDurationMs must be greater than zero.");

        if (job.RecoveryDetectorIntervalMs <= 0)
            throw new ArgumentException("recoveryDetectorIntervalMs must be greater than zero.");

        if (job.SearchAreaExpansion < 1)
            throw new ArgumentException("searchAreaExpansion must be at least 1.0.");

        if (job.MaxTrackDurationMs <= 0)
            throw new ArgumentException("maxTrackDurationMs must be greater than zero.");
    }

    private static SeedObject ResolveSeed(IReadOnlyList<AnalyzedFrame> frames, TrackForwardJob job)
    {
        if (job.SeedDetectionId.HasValue)
        {
            var seedObject = frames
                .SelectMany(frame => frame.DetectedObjects)
                .FirstOrDefault(obj => obj.Id == job.SeedDetectionId.Value);
            if (seedObject is null)
                throw new KeyNotFoundException($"Seed detection {job.SeedDetectionId.Value} not found.");

            var seedFrame = frames.First(frame => frame.Id == seedObject.AnalyzedFrameId);
            ValidateBox(seedObject.X, seedObject.Y, seedObject.Width, seedObject.Height);
            return new SeedObject(seedFrame, seedObject, WasCreated: false);
        }

        if (job.SeedBoundingBoxX is null || job.SeedBoundingBoxY is null ||
            job.SeedBoundingBoxWidth is null || job.SeedBoundingBoxHeight is null)
            throw new ArgumentException("A bounding box is required when seedDetectionId is not provided.");

        ValidateBox(
            job.SeedBoundingBoxX.Value,
            job.SeedBoundingBoxY.Value,
            job.SeedBoundingBoxWidth.Value,
            job.SeedBoundingBoxHeight.Value);

        if (string.IsNullOrWhiteSpace(job.ObjectClass))
            throw new ArgumentException("objectClass is required when seedDetectionId is not provided.");

        var explicitSeedFrame = ResolveExplicitSeedFrame(frames, job);
        var explicitSeedObject = new DetectedObject
        {
            AnalyzedFrameId = explicitSeedFrame.Id,
            Confidence = 1,
            ClassName = job.ObjectClass,
            Selected = true,
            TrackId = job.InitialTrackId,
            X = job.SeedBoundingBoxX.Value,
            Y = job.SeedBoundingBoxY.Value,
            Width = job.SeedBoundingBoxWidth.Value,
            Height = job.SeedBoundingBoxHeight.Value
        };

        explicitSeedFrame.DetectedObjects.Add(explicitSeedObject);
        return new SeedObject(explicitSeedFrame, explicitSeedObject, WasCreated: true);
    }

    private static AnalyzedFrame ResolveExplicitSeedFrame(IReadOnlyList<AnalyzedFrame> frames, TrackForwardJob job)
    {
        if (job.SeedFrameIndex.HasValue)
        {
            var frame = frames.FirstOrDefault(frame => frame.FrameIndex == job.SeedFrameIndex.Value);
            return frame ?? throw new KeyNotFoundException($"Seed frame index {job.SeedFrameIndex.Value} not found.");
        }

        var seedSeconds = job.SeedTimeMs / 1000.0;
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

    private sealed record SeedObject(AnalyzedFrame Frame, DetectedObject Object, bool WasCreated);
}
