using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Database
{
    public class Video : EntityBase
    {
        public string SourcePath { get; set; }
        public string? AnonomizedPath { get; set; }
        public string OriginalFileName { get; set; }
        public virtual ICollection<AnalyzedFrame> AnalyzedFrames { get; set; }
        public int BlurSizePercent { get; set; }
        public int TimeBufferMs { get; set; }
    }
}
