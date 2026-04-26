using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Web.Shared.DTO
{
    public class AppStateDto
    {
        public bool IsStandalone { get; set; }
        public bool ModelAvailable { get; set; }
        public bool ObjectDetectionApiRunning { get; set; }
        public CudaRuntimeStateDto? CudaRuntime { get; set; }
    }
}
