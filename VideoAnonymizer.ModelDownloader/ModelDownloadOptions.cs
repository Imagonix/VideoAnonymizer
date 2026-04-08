using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.ModelDownloader;

public class ModelDownloadOptions
{
    public string Url { get; set; } = default!;
    public string TargetPath { get; set; } = default!;
    public string? SourceModelPath { get; set; }
}