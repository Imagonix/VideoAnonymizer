using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

public static class RelevantDetectedObjectSelector
{
    internal static Dictionary<double, List<DetectedObject>> GroupObjectsByAnalyzedFrame(
        List<DetectedObject> detectedObjects)
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>();

        foreach (var obj in detectedObjects)
        {
            var timeSeconds = obj.AnalyzedFrame.TimeSeconds;

            if (!analyzedFrames.TryGetValue(timeSeconds, out var objects))
            {
                objects = [];
                analyzedFrames[timeSeconds] = objects;
            }

            objects.Add(obj);
        }

        foreach (var timeSeconds in analyzedFrames.Keys.ToList())
        {
            analyzedFrames[timeSeconds] = analyzedFrames[timeSeconds]
                .GroupBy(o => o.Id)
                .Select(g => g.Last())
                .ToList();
        }

        return analyzedFrames;
    }

    internal static List<DetectedObject> GetObjectsForFrame(
        Dictionary<double, List<DetectedObject>> analyzedFrames,
        int currentFrameIndex,
        int frameWidth,
        int frameHeight,
        double fps,
        double timeBufferSeconds,
        bool interpolateTrackedObjects)
    {
        var sourceObjects = interpolateTrackedObjects
            ? GetPredictedObjectsFromRelevantAnalyzedFrames(
                analyzedFrames,
                currentFrameIndex,
                fps,
                timeBufferSeconds)
            : GetObjectsFromRelevantAnalyzedFrames(
                analyzedFrames,
                currentFrameIndex,
                fps,
                timeBufferSeconds);

        return sourceObjects
            .Where(obj => obj.Width > 0 && obj.Height > 0)
            .Select(CopyObject)
            .ToList();
    }

    internal static List<DetectedObject> GetPredictedObjectsFromRelevantAnalyzedFrames(
        Dictionary<double, List<DetectedObject>> analyzedFrames,
        int currentFrameIndex,
        double fps,
        double timeBufferSeconds)
    {
        if (analyzedFrames.Count == 0)
            return [];

        var currentTime = currentFrameIndex / fps;
        var sortedTimes = analyzedFrames.Keys.OrderBy(t => t).ToList();
        var samplesByKey = analyzedFrames
            .SelectMany(frame => frame.Value
                .Where(obj => obj.Selected)
                .Select(obj => new TimedDetectedObject(frame.Key, obj)))
            .GroupBy(sample => GetObjectKey(sample.DetectedObject));

        var result = new List<DetectedObject>();

        foreach (var samples in samplesByKey)
        {
            var orderedSamples = samples
                .OrderBy(sample => sample.TimeSeconds)
                .ToList();

            var previous = orderedSamples.LastOrDefault(sample => sample.TimeSeconds <= currentTime);
            if (previous is null)
            {
                var upcoming = orderedSamples.FirstOrDefault(sample => sample.TimeSeconds > currentTime);
                if (upcoming is not null && IsWithinPreBuffer(currentTime, upcoming.TimeSeconds, timeBufferSeconds))
                {
                    result.Add(ProjectPreBufferObject(orderedSamples, upcoming, currentTime));
                }

                continue;
            }

            var next = orderedSamples.FirstOrDefault(sample => sample.TimeSeconds > currentTime);
            if (next is not null && previous.DetectedObject.TrackId is not null)
            {
                result.Add(ProjectObject(previous, next, currentTime, clampAlpha: true));
                continue;
            }

            var coverageEnd = GetCoverageEnd(sortedTimes, previous.TimeSeconds, timeBufferSeconds);
            if (currentTime >= previous.TimeSeconds && currentTime < coverageEnd)
            {
                result.Add(ProjectPostBufferObject(orderedSamples, previous, currentTime, timeBufferSeconds));
            }
        }

        return result;
    }

    public static List<DetectedObject> GetObjectsFromRelevantAnalyzedFrames(
        Dictionary<double, List<DetectedObject>> analyzedFrames,
        int currentFrameIndex,
        double fps,
        double timeBufferSeconds)
    {
        if (analyzedFrames.Count == 0)
            return [];

        var currentTime = currentFrameIndex / fps;
        var sortedTimes = analyzedFrames.Keys.OrderBy(t => t).ToList();

        var result = new Dictionary<string, DetectedObject>();

        for (int i = sortedTimes.Count - 1; i >= 0; i--)
        {
            var analyzedTime = sortedTimes[i];
            var nextTime = (i + 1 < sortedTimes.Count) ? sortedTimes[i + 1] : double.MaxValue;
            var coverageEnd = nextTime + timeBufferSeconds;

            if (currentTime >= analyzedTime && currentTime < coverageEnd)
            {
                foreach (var obj in analyzedFrames[analyzedTime])
                {
                    var key = obj.TrackId?.ToString() ?? obj.Id.ToString();

                    if (!result.ContainsKey(key))
                    {
                        result[key] = obj;
                    }
                }
            }
        }

        return result.Values.ToList();
    }

    private static string GetObjectKey(DetectedObject obj)
    {
        return obj.TrackId is null ? $"object-{obj.Id}" : $"track-{obj.TrackId.Value}";
    }

    private static double GetCoverageEnd(
        IReadOnlyList<double> sortedTimes,
        double analyzedTime,
        double timeBufferSeconds)
    {
        foreach (var time in sortedTimes)
        {
            if (time > analyzedTime)
                return time + timeBufferSeconds;
        }

        return double.MaxValue;
    }

    private static bool IsWithinPreBuffer(
        double currentTime,
        double analyzedTime,
        double timeBufferSeconds)
    {
        return timeBufferSeconds > 0
            && currentTime >= analyzedTime - timeBufferSeconds
            && currentTime < analyzedTime;
    }

    private static DetectedObject ProjectPreBufferObject(
        IReadOnlyList<TimedDetectedObject> orderedSamples,
        TimedDetectedObject upcoming,
        double currentTime)
    {
        if (upcoming.DetectedObject.TrackId is null)
            return CopyObject(upcoming.DetectedObject);

        var next = orderedSamples.FirstOrDefault(sample => sample.TimeSeconds > upcoming.TimeSeconds);
        return next is null
            ? CopyObject(upcoming.DetectedObject)
            : ProjectObject(upcoming, next, currentTime, clampAlpha: false);
    }

    private static DetectedObject ProjectPostBufferObject(
        IReadOnlyList<TimedDetectedObject> orderedSamples,
        TimedDetectedObject previous,
        double currentTime,
        double timeBufferSeconds)
    {
        if (previous.DetectedObject.TrackId is null
            || timeBufferSeconds <= 0
            || currentTime > previous.TimeSeconds + timeBufferSeconds)
        {
            return CopyObject(previous.DetectedObject);
        }

        var prior = orderedSamples.LastOrDefault(sample => sample.TimeSeconds < previous.TimeSeconds);
        return prior is null
            ? CopyObject(previous.DetectedObject)
            : ProjectObject(prior, previous, currentTime, clampAlpha: false);
    }

    private static DetectedObject ProjectObject(
        TimedDetectedObject previous,
        TimedDetectedObject next,
        double currentTime,
        bool clampAlpha)
    {
        var duration = next.TimeSeconds - previous.TimeSeconds;
        if (duration <= 0)
            return CopyObject(previous.DetectedObject);

        var alpha = (currentTime - previous.TimeSeconds) / duration;
        if (clampAlpha)
        {
            alpha = Math.Clamp(alpha, 0, 1);
        }

        var previousBox = previous.DetectedObject;
        var nextBox = next.DetectedObject;

        var centerX = Lerp(previousBox.X + previousBox.Width / 2.0, nextBox.X + nextBox.Width / 2.0, alpha);
        var centerY = Lerp(previousBox.Y + previousBox.Height / 2.0, nextBox.Y + nextBox.Height / 2.0, alpha);
        var width = Lerp(previousBox.Width, nextBox.Width, alpha);
        var height = Lerp(previousBox.Height, nextBox.Height, alpha);

        var left = (int)Math.Floor(centerX - width / 2.0);
        var top = (int)Math.Floor(centerY - height / 2.0);
        var right = (int)Math.Ceiling(centerX + width / 2.0);
        var bottom = (int)Math.Ceiling(centerY + height / 2.0);
        var metadataSource = alpha < 0.5 ? previousBox : nextBox;

        return new DetectedObject
        {
            Id = metadataSource.Id,
            Confidence = Math.Clamp(Lerp(previousBox.Confidence, nextBox.Confidence, alpha), 0, 1),
            ClassName = metadataSource.ClassName,
            BlurShape = string.IsNullOrWhiteSpace(metadataSource.BlurShape)
                ? previousBox.BlurShape ?? nextBox.BlurShape
                : metadataSource.BlurShape,
            Selected = true,
            TrackId = previousBox.TrackId,
            X = left,
            Y = top,
            Width = Math.Max(0, right - left),
            Height = Math.Max(0, bottom - top),
            AnalyzedFrameId = metadataSource.AnalyzedFrameId
        };
    }

    private static DetectedObject CopyObject(DetectedObject source)
    {
        return new DetectedObject
        {
            Id = source.Id,
            Confidence = source.Confidence,
            ClassName = source.ClassName,
            BlurShape = source.BlurShape,
            Selected = source.Selected,
            TrackId = source.TrackId,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            AnalyzedFrameId = source.AnalyzedFrameId
        };
    }

    private static double Lerp(double start, double end, double alpha)
    {
        return start + (end - start) * alpha;
    }

    private sealed record TimedDetectedObject(double TimeSeconds, DetectedObject DetectedObject);
}
