using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Web.Shared.DTO
{
    public class AppStateDto
    {
        public bool ModelAvailable { get; set; }
        public bool ObjectDetectionApiRunning { get; set; }
    }
}
