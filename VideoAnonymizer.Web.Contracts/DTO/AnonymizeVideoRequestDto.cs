using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Web.Shared.DTO
{
    public class AnonymizeVideoRequestDto
    {
        public List<AnalyzedFrameDto> Frames { get; set; } = new();
        public AnonymizationSettingsDto? Settings { get; set; }
    }
}
