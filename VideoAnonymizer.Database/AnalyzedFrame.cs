using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Database
{
    public class AnalyzedFrame : EntityBase
    {
        public double TimeSeconds { get; set; }
        public virtual ICollection<DetectedObject> DetectedObjects { get; set; }
        public virtual Guid VideoId { get; set; }
        public virtual Video Video { get; set; } = default!;
    }
}
