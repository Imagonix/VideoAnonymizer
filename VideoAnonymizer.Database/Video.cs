using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Database
{
    public class Video : EntityBase
    {
        public string Path { get; set; }
        public virtual ICollection<AnalyzedFrame> AnalyzedFrames { get; set; }
    }
}
