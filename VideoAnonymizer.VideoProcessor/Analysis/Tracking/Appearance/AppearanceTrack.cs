namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

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
