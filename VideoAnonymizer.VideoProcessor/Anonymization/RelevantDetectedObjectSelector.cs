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
        double timeBufferSeconds)
    {
        var sourceObjects = GetObjectsFromRelevantAnalyzedFrames(
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
}
