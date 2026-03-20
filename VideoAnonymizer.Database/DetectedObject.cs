using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace VideoAnonymizer.Database
{
    public class DetectedObject : EntityBase
    {
        public double Confidence { get; set; }
        public string? ClassName { get; set; }
        public bool Selected { get; set; }
        public string? TrackId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Guid AnalyzedFrameId { get; set; }
        [JsonIgnore]
        public AnalyzedFrame AnalyzedFrame { get; set; } = default!;
    }
}
