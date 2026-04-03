using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.VideoProcessor
{
    internal class AnonymizeVideoConsumer(VideoAnonymizer Worker) : IConsumer<AnonymizeVideo>
    {
        public async Task Consume(ConsumeContext<AnonymizeVideo> context)
        {
            await Worker.EnqueueAsync(context.Message);
        }
    }
}
