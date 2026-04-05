using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Web.Services;

namespace VideoAnonymizer.Web.Tests.FakeServices;

public sealed class FakeDownloadService : IDownloadService
{
    public string? LastDownloadedFileName { get; private set; }
    public string? LastDownloadedUrl { get; private set; }
    public int DownloadCallCount { get; private set; }

    public Task DownloadFileAsync(string fileName, string url)
    {
        LastDownloadedFileName = fileName;
        LastDownloadedUrl = url;
        DownloadCallCount++;
        return Task.CompletedTask;
    }
}
