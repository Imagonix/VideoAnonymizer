namespace VideoAnonymizer.Web.Services;

public interface IDownloadService
{
    Task DownloadFileAsync(string fileName, string url);
}
