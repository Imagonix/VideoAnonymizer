namespace VideoAnonymizer.ApiService.DataServices;

public interface IObjectDetectionApiReadyState
{
    bool IsReady { get; }
    void Set(bool value);
}