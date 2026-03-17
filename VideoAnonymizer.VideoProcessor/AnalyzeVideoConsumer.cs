using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.VideoProcessor
{
    internal class AnalyzeVideoConsumer(VideoAnalyzer Worker) : IConsumer<AnalyzeVideo>
    {
        public async Task Consume(ConsumeContext<AnalyzeVideo> context)
        {
            Worker.
        }
    }
}
