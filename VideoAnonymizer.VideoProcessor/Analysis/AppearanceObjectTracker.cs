namespace VideoAnonymizer.VideoProcessor.Analysis;

internal sealed class AppearanceObjectTracker(AppearanceObjectTrackingOptions options)
{
    private readonly List<AppearanceTrack> _tracks = [];
    private int _nextTrackId = 1;

    public IReadOnlyDictionary<Guid, int> AssignTracks(
        int frameIndex,
        double timeSeconds,
        IReadOnlyList<AppearanceDetection> detections)
    {
        var assignments = new Dictionary<Guid, int>();
        var candidates = BuildCandidates(frameIndex, timeSeconds, detections)
            .OrderByDescending(candidate => candidate.Score)
            .ToList();
        var assignedDetectionIndexes = new HashSet<int>();
        var assignedTrackIds = new HashSet<int>();

        foreach (var candidate in candidates)
        {
            if (assignedDetectionIndexes.Contains(candidate.DetectionIndex) ||
                assignedTrackIds.Contains(candidate.Track.Id))
            {
                continue;
            }

            var detection = detections[candidate.DetectionIndex];
            assignments[detection.DetectedObjectId] = candidate.Track.Id;
            assignedDetectionIndexes.Add(candidate.DetectionIndex);
            assignedTrackIds.Add(candidate.Track.Id);
        }

        for (var i = 0; i < detections.Count; i++)
        {
            if (assignedDetectionIndexes.Contains(i))
                continue;

            var track = new AppearanceTrack(
                _nextTrackId++,
                detections[i].ClassName,
                options.MaxSamplesPerTrack);
            _tracks.Add(track);
            assignments[detections[i].DetectedObjectId] = track.Id;
        }

        foreach (var detection in detections)
        {
            var trackId = assignments[detection.DetectedObjectId];
            var track = _tracks.Single(track => track.Id == trackId);
            track.Update(frameIndex, timeSeconds, detection);
        }

        return assignments;
    }

    private IEnumerable<AssignmentCandidate> BuildCandidates(
        int frameIndex,
        double timeSeconds,
        IReadOnlyList<AppearanceDetection> detections)
    {
        for (var detectionIndex = 0; detectionIndex < detections.Count; detectionIndex++)
        {
            var detection = detections[detectionIndex];
            if (detection.AppearanceFeature is null)
                continue;

            foreach (var track in _tracks)
            {
                if (!string.Equals(track.ClassName, detection.ClassName, StringComparison.Ordinal))
                    continue;

                var gapSeconds = timeSeconds - track.LastTimeSeconds;
                if (gapSeconds < 0 || gapSeconds > options.MaxTrackGapSeconds)
                    continue;

                var appearanceSimilarity = track.CalculateBestAppearanceSimilarity(detection.AppearanceFeature);
                if (appearanceSimilarity < options.MinAppearanceSimilarity)
                    continue;

                var spatialSimilarity = CalculateSpatialSimilarity(track.LastBox, detection.Box);
                if (spatialSimilarity < options.MinSpatialSimilarity)
                    continue;

                var sizeSimilarity = CalculateSizeSimilarity(track.LastBox, detection.Box);
                if (sizeSimilarity < options.MinSizeSimilarity)
                    continue;

                var recencyScore = options.MaxTrackGapSeconds <= 0
                    ? 1
                    : 1 - Math.Min(1, gapSeconds / options.MaxTrackGapSeconds);
                var score = CalculateAssignmentScore(
                    appearanceSimilarity,
                    spatialSimilarity,
                    sizeSimilarity,
                    recencyScore);
                if (score < options.MinAssignmentScore)
                    continue;

                yield return new AssignmentCandidate(detectionIndex, track, score);
            }
        }
    }

    private double CalculateSpatialSimilarity(TrackBox previous, TrackBox current)
    {
        var previousCenter = previous.Center;
        var currentCenter = current.Center;
        var centerDistance = Math.Sqrt(
            Math.Pow(previousCenter.X - currentCenter.X, 2) +
            Math.Pow(previousCenter.Y - currentCenter.Y, 2));
        var averageDiagonal = Math.Max(1, (previous.Diagonal + current.Diagonal) / 2);
        var maxDistance = averageDiagonal * options.MaxCenterDistanceBoxDiagonals;

        return 1 - Math.Min(1, centerDistance / maxDistance);
    }

    private static double CalculateSizeSimilarity(TrackBox previous, TrackBox current)
    {
        var widthSimilarity = RatioSimilarity(previous.Width, current.Width);
        var heightSimilarity = RatioSimilarity(previous.Height, current.Height);
        var areaSimilarity = RatioSimilarity(previous.Area, current.Area);

        return (widthSimilarity + heightSimilarity + areaSimilarity) / 3;
    }

    private static double RatioSimilarity(double previous, double current)
    {
        if (previous <= 0 || current <= 0)
            return 0;

        return Math.Min(previous, current) / Math.Max(previous, current);
    }

    private double CalculateAssignmentScore(
        double appearanceSimilarity,
        double spatialSimilarity,
        double sizeSimilarity,
        double recencyScore)
    {
        var totalWeight = options.AppearanceScoreWeight
            + options.SpatialScoreWeight
            + options.SizeScoreWeight
            + options.RecencyScoreWeight;
        if (totalWeight <= 0)
            return 0;

        return (
            appearanceSimilarity * options.AppearanceScoreWeight +
            spatialSimilarity * options.SpatialScoreWeight +
            sizeSimilarity * options.SizeScoreWeight +
            recencyScore * options.RecencyScoreWeight) / totalWeight;
    }

    private sealed record AssignmentCandidate(
        int DetectionIndex,
        AppearanceTrack Track,
        double Score);
}

internal sealed class AppearanceTrack(
    int id,
    string className,
    int maxSamplesPerTrack)
{
    private readonly Queue<AppearanceTrackSample> _samples = [];

    public int Id { get; } = id;
    public string ClassName { get; } = className;
    public TrackBox LastBox { get; private set; }
    public int LastFrameIndex { get; private set; } = -1;
    public double LastTimeSeconds { get; private set; }

    public double CalculateBestAppearanceSimilarity(AppearanceFeature feature)
    {
        return _samples
            .Select(sample => AppearanceFeature.Compare(sample.Feature, feature))
            .DefaultIfEmpty(0)
            .Max();
    }

    public void Update(
        int frameIndex,
        double timeSeconds,
        AppearanceDetection detection)
    {
        LastFrameIndex = frameIndex;
        LastTimeSeconds = timeSeconds;
        LastBox = detection.Box;

        if (detection.AppearanceFeature is null)
            return;

        _samples.Enqueue(new AppearanceTrackSample(detection.AppearanceFeature));
        while (_samples.Count > maxSamplesPerTrack)
            _samples.Dequeue();
    }
}

internal sealed record AppearanceTrackSample(AppearanceFeature Feature);

internal sealed record AppearanceDetection(
    Guid DetectedObjectId,
    string ClassName,
    double Confidence,
    TrackBox Box,
    AppearanceFeature? AppearanceFeature);

internal readonly record struct TrackBox(int X, int Y, int Width, int Height)
{
    public double Area => Math.Max(0, (double)Width) * Math.Max(0, Height);
    public double Diagonal => Math.Sqrt((double)Width * Width + (double)Height * Height);
    public TrackPoint Center => new(X + Width / 2.0, Y + Height / 2.0);
}

internal readonly record struct TrackPoint(double X, double Y);
