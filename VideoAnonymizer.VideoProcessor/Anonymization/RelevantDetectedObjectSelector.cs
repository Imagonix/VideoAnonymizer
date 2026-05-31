using OpenCvSharp;
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

        var result = new List<DetectedObject>();

        foreach (var obj in sourceObjects)
        {
            var rect = ClampRect(
                new Rect(obj.X, obj.Y, obj.Width, obj.Height),
                frameWidth,
                frameHeight);

            if (rect.Width > 0 && rect.Height > 0)
            {
                result.Add(new DetectedObject
                {
                    Id = obj.Id,
                    TrackId = obj.TrackId,
                    BlurShape = obj.BlurShape,
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height
                });
            }
        }

        return result;
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
                    result.Add(CopyObject(upcoming.DetectedObject));
                }

                continue;
            }

            var next = orderedSamples.FirstOrDefault(sample => sample.TimeSeconds > currentTime);
            if (next is not null && previous.DetectedObject.TrackId is not null)
            {
                result.Add(InterpolateObject(previous, next, currentTime));
                continue;
            }

            var coverageEnd = GetCoverageEnd(sortedTimes, previous.TimeSeconds, timeBufferSeconds);
            if (currentTime >= previous.TimeSeconds && currentTime < coverageEnd)
            {
                result.Add(CopyObject(previous.DetectedObject));
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

    private static Rect ClampRect(Rect rect, int maxWidth, int maxHeight)
    {
        var x = Math.Max(0, rect.X);
        var y = Math.Max(0, rect.Y);

        var right = Math.Min(maxWidth, rect.X + rect.Width);
        var bottom = Math.Min(maxHeight, rect.Y + rect.Height);

        var width = Math.Max(0, right - x);
        var height = Math.Max(0, bottom - y);

        return new Rect(x, y, width, height);
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

    private static DetectedObject InterpolateObject(
        TimedDetectedObject previous,
        TimedDetectedObject next,
        double currentTime)
    {
        var duration = next.TimeSeconds - previous.TimeSeconds;
        if (duration <= 0)
            return CopyObject(previous.DetectedObject);

        var alpha = Math.Clamp((currentTime - previous.TimeSeconds) / duration, 0, 1);
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
            Confidence = Lerp(previousBox.Confidence, nextBox.Confidence, alpha),
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
