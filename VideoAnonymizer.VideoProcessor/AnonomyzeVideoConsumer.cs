using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.VideoProcessor
{
    internal class AnonomyzeVideoConsumer(VideoAnonomyzer Worker) : IConsumer<AnonomyzeVideo>
    {
        public async Task Consume(ConsumeContext<AnonomyzeVideo> context)
        {
            await Worker.EnqueueAsync(context.Message);
        }
    }
}
