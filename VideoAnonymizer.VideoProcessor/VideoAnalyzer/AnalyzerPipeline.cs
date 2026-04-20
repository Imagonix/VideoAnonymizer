using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Channels;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor.VideoAnalyzer
{
    public class AnalyzerPipeline(ILogger<AnalyzerPipeline> logger)
    {

        internal async Task<int> Run(
            VideoCapture capture,
            double fps,
            int frameStep,
            int lastFrameIndex,
            Video video,
            IDbContextFactory<VideoAnonymizerDbContext> dbContextFactory,
            ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
            string sessionId,
            CancellationToken stoppingToken)
        {
            var frameChannel = Channel.CreateBounded<FrameWorkItem>(new BoundedChannelOptions(20)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false
            });

            var resultChannel = Channel.CreateBounded<DetectionWorkResult>(new BoundedChannelOptions(20)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });

            const int workerCount = 4;

            var producerTask = Task.Run(async () =>
            {
                try
                {
                    using var frame = new Mat();
                    var db = await dbContextFactory.CreateDbContextAsync();
                    var frameIndex = 0;
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var success = capture.Read(frame);
                        if (!success || frame.Empty())
                            break;

                        var shouldProcess =
                            frameIndex == 0 ||
                            frameIndex == lastFrameIndex ||
                            frameIndex % frameStep == 0;

                        if (!shouldProcess)
                        {
                            frameIndex++;
                            continue;
                        }

                        var analyzedFrame = new AnalyzedFrame
                        {
                            TimeSeconds = frameIndex / fps,
                            VideoId = video.Id
                        };

                        await db.AddAsync(analyzedFrame, stoppingToken);
                        await db.SaveChangesAsync(stoppingToken);

                        var imageBase64 = ConvertMatToBase64Jpeg(frame);

                        await frameChannel.Writer.WriteAsync(
                            new FrameWorkItem(
                                FrameIndex: frameIndex,
                                TimestampSeconds: frameIndex / fps,
                                VideoId: video.Id,
                                AnalyzedFrameId: analyzedFrame.Id,
                                ImageBase64: imageBase64),
                            stoppingToken);

                        frameIndex++;
                    }
                }
                finally
                {
                    frameChannel.Writer.Complete();
                }
            }, stoppingToken);

            var workerTasks = Enumerable.Range(0, workerCount)
                .Select(_ => Task.Run(async () =>
                {
                    await foreach (var item in frameChannel.Reader.ReadAllAsync(stoppingToken))
                    {
                        var detections = await objectDetectionClient.DetectObjects_detectObjects_postAsync(
                            new DetectRequest
                            {
                                ImageBase64 = item.ImageBase64,
                                SessionId = sessionId,
                            },
                            stoppingToken);

                        var detectedObjects = detections.Select(detection => new DetectedObject
                        {
                            Selected = true,
                            AnalyzedFrameId = item.AnalyzedFrameId,
                            Height = detection.Height,
                            Width = detection.Width,
                            X = detection.X,
                            Y = detection.Y,
                            ClassName = detection.ClassName,
                            Confidence = detection.Confidence,
                            TrackId = detection.TrackId,
                        }).ToList();

                        await resultChannel.Writer.WriteAsync(
                            new DetectionWorkResult(
                                FrameIndex: item.FrameIndex,
                                TimestampSeconds: item.TimestampSeconds,
                                AnalyzedFrameId: item.AnalyzedFrameId,
                                DetectedObjects: detectedObjects,
                                DetectionCount: detections?.Count ?? 0),
                            stoppingToken);
                    }
                }, stoppingToken))
                .ToArray();

            var completeResultsTask = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(workerTasks);
                    resultChannel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    resultChannel.Writer.Complete(ex);
                    throw;
                }
            }, stoppingToken);

            var saverTask = Task.Run(async () =>
            {
                const int batchSize = 5000;

                var processedFrameCount = 0;
                var bufferedDetectedObjects = new List<DetectedObject>(batchSize);

                await using var db = await dbContextFactory.CreateDbContextAsync(stoppingToken);

                await foreach (var result in resultChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    if (result.DetectedObjects.Count > 0)
                    {
                        bufferedDetectedObjects.AddRange(result.DetectedObjects);
                    }

                    logger.LogInformation(
                        "Frame {FrameIndex} at {Timestamp} processed. Detections: {Count}",
                        result.FrameIndex,
                        TimeSpan.FromSeconds(result.TimestampSeconds),
                        result.DetectionCount);

                    processedFrameCount++;

                    if (bufferedDetectedObjects.Count >= batchSize)
                    {
                        await db.AddRangeAsync(bufferedDetectedObjects, stoppingToken);
                        await db.SaveChangesAsync(stoppingToken);
                        bufferedDetectedObjects.Clear();
                    }
                }

                if (bufferedDetectedObjects.Count > 0)
                {
                    await db.AddRangeAsync(bufferedDetectedObjects, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);
                }

                return processedFrameCount;
            }, stoppingToken);

            await producerTask;
            await completeResultsTask;
            var processedFrameCount = await saverTask;
            return processedFrameCount;
        }

        private static string ConvertMatToBase64Jpeg(Mat frame)
        {
            Cv2.ImEncode(".jpg", frame, out var imageBytes);
            return Convert.ToBase64String(imageBytes);
        }
    }
}
