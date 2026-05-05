namespace VideoAnonymizer.ApiService.Controllers;

internal static class VideoFileContentTypes
{
    public static string FromPath(string videoPath)
    {
        return videoPath.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ? "video/webm" :
            videoPath.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ? "video/quicktime" :
            "video/mp4";
    }
}
