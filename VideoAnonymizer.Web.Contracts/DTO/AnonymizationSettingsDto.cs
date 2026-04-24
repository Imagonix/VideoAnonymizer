using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Web.Shared.DTO
{
    public class AnonymizationSettingsDto
    {
        public int BlurSizePercent { get; set; }
        public int TimeBufferMs { get; set; }
    }
}
