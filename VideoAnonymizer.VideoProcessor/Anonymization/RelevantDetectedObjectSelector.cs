using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

public static class RelevantDetectedObjectSelector
{
    public static List<DetectedObject> GetObjectsForFrame(
        IReadOnlyList<AnalyzedFrame> analyzedFrames,
        int currentFrameIndex,
        double fps,
        int globalTimeBufferMs,
        bool interpolateTrackedObjects,
        int videoWidth = 0,
        int videoHeight = 0)
    {
        var currentTime = currentFrameIndex / fps;
        var segments = ConsecutiveSegmentResolver.BuildAll(analyzedFrames);

        var sourceObjects = interpolateTrackedObjects
            ? GetPredictedObjects(segments, currentTime, globalTimeBufferMs)
            : GetHeldObjects(analyzedFrames, segments, currentTime, globalTimeBufferMs);

        return sourceObjects
            .Where(obj => obj.Selected && obj.Width > 0 && obj.Height > 0)
            .Select(CopyObject)
            // Keep raw unbounded geometry for export; BlurRegion intersects at rasterization.
            // Drop only boxes whose raw region is fully outside when frame size is known.
            .Where(obj => !ProjectedRegionClipper.IsFullyOutside(obj, videoWidth, videoHeight))
            .ToList();
    }

    private static List<DetectedObject> GetPredictedObjects(
        IReadOnlyList<ConsecutiveSegment> segments,
        double currentTime,
        int globalTimeBufferMs)
    {
        var result = new List<DetectedObject>();
        var trackedGroups = segments
            .Where(segment => segment.First.TrackId is not null)
            .GroupBy(segment => segment.First.TrackId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(segment => segment.First.AnalyzedFrame.FrameIndex).ToList());

        foreach (var segment in segments)
        {
            var selectedOccurrences = segment.Occurrences.Where(obj => obj.Selected).ToList();
            if (selectedOccurrences.Count == 0)
                continue;

            var (preBufferMs, postBufferMs) = AnonymizationSettingsResolver.ResolveBuffers(segment, globalTimeBufferMs);
            var preBufferSeconds = Math.Max(0, preBufferMs) / 1000.0;
            var postBufferSeconds = Math.Max(0, postBufferMs) / 1000.0;

            var first = selectedOccurrences[0];
            var last = selectedOccurrences[^1];
            var firstTime = first.AnalyzedFrame.TimeSeconds;
            var lastTime = last.AnalyzedFrame.TimeSeconds;

            var precedingGapInterpolates = false;
            var followingGapInterpolates = false;
            if (segment.First.TrackId is int trackId
                && trackedGroups.TryGetValue(trackId, out var trackSegments))
            {
                var index = trackSegments.FindIndex(candidate => ReferenceEquals(candidate, segment)
                    || candidate.Occurrences.Any(obj => obj.Id == segment.First.Id));
                if (index > 0)
                {
                    precedingGapInterpolates = !GapHandlingModes.IsUseBuffers(
                        trackSegments[index - 1].Last.NextGapHandlingMode);
                }

                if (index >= 0 && index < trackSegments.Count - 1)
                {
                    followingGapInterpolates = !GapHandlingModes.IsUseBuffers(
                        segment.Last.NextGapHandlingMode);
                }
            }

            // Internal Interpolate gaps ignore the bordering After/Before buffers.
            var effectivePreSeconds = precedingGapInterpolates ? 0 : preBufferSeconds;
            var effectivePostSeconds = followingGapInterpolates ? 0 : postBufferSeconds;

            if (currentTime < firstTime - effectivePreSeconds)
                continue;

            if (currentTime < firstTime)
            {
                if (effectivePreSeconds <= 0)
                    continue;

                result.Add(ProjectPreBufferObject(selectedOccurrences, currentTime));
                continue;
            }

            if (currentTime > lastTime)
            {
                if (effectivePostSeconds <= 0 || currentTime > lastTime + effectivePostSeconds)
                    continue;

                // Keep projecting from the segment-boundary motion model for the full
                // post-buffer. Never fall back to a historical stored position.
                result.Add(ProjectPostBufferObject(selectedOccurrences, currentTime));
                continue;
            }

            result.Add(InterpolateInSegment(selectedOccurrences, currentTime));
        }

        // Default Interpolate bridges each real same-track gap with one current-time region.
        foreach (var trackSegments in trackedGroups.Values)
        {
            for (var index = 0; index < trackSegments.Count - 1; index++)
            {
                var previous = trackSegments[index];
                var next = trackSegments[index + 1];
                if (GapHandlingModes.IsUseBuffers(previous.Last.NextGapHandlingMode))
                    continue;

                var previousBoundary = previous.Last;
                var nextBoundary = next.First;
                if (!previousBoundary.Selected || !nextBoundary.Selected)
                    continue;

                var previousTime = previousBoundary.AnalyzedFrame.TimeSeconds;
                var nextTime = nextBoundary.AnalyzedFrame.TimeSeconds;
                if (currentTime <= previousTime || currentTime >= nextTime)
                    continue;

                result.Add(ProjectObject(previousBoundary, nextBoundary, currentTime, clampAlpha: true));
            }
        }

        return result;
    }

    private static List<DetectedObject> GetHeldObjects(
        IReadOnlyList<AnalyzedFrame> analyzedFrames,
        IReadOnlyList<ConsecutiveSegment> segments,
        double currentTime,
        int globalTimeBufferMs)
    {
        var frameTimes = analyzedFrames
            .Select(frame => frame.TimeSeconds)
            .Distinct()
            .OrderBy(time => time)
            .ToList();

        var result = new Dictionary<string, DetectedObject>();

        var occurrences = segments
            .SelectMany(segment => segment.Occurrences
                .Where(obj => obj.Selected)
                .Select(obj => (Object: obj, Segment: segment)))
            .OrderByDescending(item => item.Object.AnalyzedFrame.FrameIndex)
            .ToList();

        foreach (var item in occurrences)
        {
            var key = item.Object.TrackId is null
                ? $"object-{item.Object.Id}"
                : $"track-{item.Object.TrackId.Value}";
            if (result.ContainsKey(key))
                continue;

            var (_, postBufferMs) = AnonymizationSettingsResolver.ResolveBuffers(item.Segment, globalTimeBufferMs);
            var postBufferSeconds = Math.Max(0, postBufferMs) / 1000.0;
            var analyzedTime = item.Object.AnalyzedFrame.TimeSeconds;
            var coverageEnd = NextFrameTime(frameTimes, analyzedTime) + postBufferSeconds;

            if (currentTime >= analyzedTime && currentTime < coverageEnd)
                result[key] = item.Object;
        }

        return result.Values.ToList();
    }

    private static double NextFrameTime(IReadOnlyList<double> sortedFrameTimes, double analyzedTime)
    {
        foreach (var time in sortedFrameTimes)
        {
            if (time > analyzedTime)
                return time;
        }

        return double.MaxValue;
    }

    private static DetectedObject InterpolateInSegment(
        IReadOnlyList<DetectedObject> selectedOccurrences,
        double currentTime)
    {
        var previous = selectedOccurrences
            .LastOrDefault(obj => obj.AnalyzedFrame.TimeSeconds <= currentTime);
        if (previous is null)
            return CopyObject(selectedOccurrences[0]);

        if (previous.AnalyzedFrame.TimeSeconds == currentTime)
            return CopyObject(previous);

        var next = selectedOccurrences
            .FirstOrDefault(obj => obj.AnalyzedFrame.TimeSeconds > currentTime);
        return next is null
            ? CopyObject(previous)
            : ProjectObject(previous, next, currentTime, clampAlpha: true);
    }

    private static DetectedObject ProjectPreBufferObject(
        IReadOnlyList<DetectedObject> selectedOccurrences,
        double currentTime)
    {
        var first = selectedOccurrences[0];
        if (selectedOccurrences.Count == 1)
            return CopyObject(first);

        return ProjectObject(first, selectedOccurrences[1], currentTime, clampAlpha: false);
    }

    private static DetectedObject ProjectPostBufferObject(
        IReadOnlyList<DetectedObject> selectedOccurrences,
        double currentTime)
    {
        var last = selectedOccurrences[^1];
        if (selectedOccurrences.Count == 1)
            return CopyObject(last);

        return ProjectObject(selectedOccurrences[^2], last, currentTime, clampAlpha: false);
    }

    private static DetectedObject ProjectObject(
        DetectedObject previous,
        DetectedObject next,
        double currentTime,
        bool clampAlpha)
    {
        var previousTime = previous.AnalyzedFrame.TimeSeconds;
        var nextTime = next.AnalyzedFrame.TimeSeconds;
        var duration = nextTime - previousTime;
        if (duration <= 0)
            return CopyObject(previous);

        var alpha = (currentTime - previousTime) / duration;
        if (clampAlpha)
        {
            alpha = Math.Clamp(alpha, 0, 1);
        }

        var centerX = Lerp(previous.X + previous.Width / 2.0, next.X + next.Width / 2.0, alpha);
        var centerY = Lerp(previous.Y + previous.Height / 2.0, next.Y + next.Height / 2.0, alpha);
        var width = Lerp(previous.Width, next.Width, alpha);
        var height = Lerp(previous.Height, next.Height, alpha);

        var left = (int)Math.Floor(centerX - width / 2.0);
        var top = (int)Math.Floor(centerY - height / 2.0);
        var right = (int)Math.Ceiling(centerX + width / 2.0);
        var bottom = (int)Math.Ceiling(centerY + height / 2.0);
        var metadataSource = alpha < 0.5 ? previous : next;

        return new DetectedObject
        {
            Id = metadataSource.Id,
            Confidence = Math.Clamp(Lerp(previous.Confidence, next.Confidence, alpha), 0, 1),
            ClassName = metadataSource.ClassName,
            BlurShape = string.IsNullOrWhiteSpace(metadataSource.BlurShape)
                ? previous.BlurShape ?? next.BlurShape
                : metadataSource.BlurShape,
            BlurSizePercentOverride = metadataSource.BlurSizePercentOverride,
            OccurrenceBlurSizePercentOverride = metadataSource.OccurrenceBlurSizePercentOverride,
            PreBufferMsOverride = metadataSource.PreBufferMsOverride,
            PostBufferMsOverride = metadataSource.PostBufferMsOverride,
            NextGapHandlingMode = metadataSource.NextGapHandlingMode,
            Selected = true,
            TrackId = previous.TrackId,
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
            BlurSizePercentOverride = source.BlurSizePercentOverride,
            OccurrenceBlurSizePercentOverride = source.OccurrenceBlurSizePercentOverride,
            PreBufferMsOverride = source.PreBufferMsOverride,
            PostBufferMsOverride = source.PostBufferMsOverride,
            NextGapHandlingMode = source.NextGapHandlingMode,
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
}
