namespace VideoAnonymizer.VideoProcessor.Analysis;

internal sealed class ConsecutiveFrameTracker
{
    private readonly int _frameStep;
    private readonly HashSet<int> _saved = [];
    private int _maxConsecutive;
    private readonly object _lock = new();

    public ConsecutiveFrameTracker(int frameStep)
    {
        _frameStep = Math.Max(1, frameStep);
        _maxConsecutive = -_frameStep;
    }

    public void ReportSaved(IEnumerable<int> frameIndices)
    {
        lock (_lock)
        {
            foreach (var idx in frameIndices)
                _saved.Add(idx);

            while (_saved.Contains(_maxConsecutive + _frameStep))
                _maxConsecutive += _frameStep;
        }
    }

    public int MaxConsecutive
    {
        get { lock (_lock) { return _maxConsecutive; } }
    }
}