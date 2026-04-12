namespace VideoAnonymizer.ApiService.DataServices;

public class ObjectDetectionApiReadyState : IObjectDetectionApiReadyState
{
    private volatile bool _isReady;

    public bool IsReady => _isReady;

    public void Set(bool value)
    {
        _isReady = value;
    }
}
