using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Database
{
    public class Video : EntityBase
    {
        public string SourcePath { get; set; }
        public string? AnonomizedPath { get; set; }
        public virtual ICollection<AnalyzedFrame> AnalyzedFrames { get; set; }
    }
}
