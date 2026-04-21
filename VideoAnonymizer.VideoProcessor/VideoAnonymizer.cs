using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor;

public class VideoAnonymizer(
    ILogger<VideoAnonymizer> logger,
    IMessagePublisher messagePublisher,
    IServiceProvider serviceProvider)
    : SingleJobQueingWorker<AnonymizeVideo>(logger)
{
    protected override async Task HandleJob(AnonymizeVideo job, CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<VideoAnonymizerDbContext>();

        var video = await db.Videos.FindAsync(job.VideoId, stoppingToken);
        if (video is null)
            throw new InvalidOperationException($"Video '{job.VideoId}' not found");

        var selectedObjects = await db.DetectedObjects
            .AsNoTracking()
            .Where(o => o.Selected && o.AnalyzedFrame.VideoId == video.Id)
            .Include(o => o.AnalyzedFrame)
            .OrderBy(o => o.AnalyzedFrame.TimeSeconds)
            .ToListAsync(stoppingToken);

        if (!File.Exists(video.SourcePath))
            throw new FileNotFoundException($"Source video not found: {video.SourcePath}");

        var sourcePath = video.SourcePath;
        var outputPath = BuildOutputPath(sourcePath);

        using var capture = new VideoCapture(sourcePath);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Could not open video '{sourcePath}'");

        var fps = capture.Fps > 0 ? capture.Fps : 25;
        var frameWidth = capture.FrameWidth;
        var frameHeight = capture.FrameHeight;

        var totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);

        // using block to ensure file is written completly, when exiting the block
        using (var writer = new VideoWriter(outputPath, GetSafeFourCc(capture), fps, new Size(frameWidth, frameHeight)))
        {
            var analyzedFrames = GroupObjectsByAnalyzedFrame(selectedObjects, fps, totalFrames);

            using var frameMat = new Mat();
            var currentFrameIndex = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!capture.Read(frameMat) || frameMat.Empty())
                    break;

                var objectsToBlur = GetObjectsForFrame(analyzedFrames, currentFrameIndex, frameWidth, frameHeight);

                foreach (var obj in objectsToBlur)
                {
                    BlurRegion(frameMat, obj);
                }

                writer.Write(frameMat);
                currentFrameIndex++;
            }
        }

        video.AnonomizedPath = outputPath;
        await db.SaveChangesAsync(stoppingToken);

        await messagePublisher.PublishAsync(
            RabbitMQConstants.RoutingKeys.Anonymized,
            new AnonymizedVideo(job.JobId, DateTime.Now),
            stoppingToken);
    }

    private static Dictionary<int, List<DetectedObject>> GroupObjectsByAnalyzedFrame(
        List<DetectedObject> detectedObjects,
        double fps,
        int? totalFrames = null)
    {
        var analyzedFrames = new Dictionary<int, List<DetectedObject>>();
        var lastValidFrameIndex = totalFrames.HasValue
            ? Math.Max(0, totalFrames.Value - 1)
            : (int?)null;

        foreach (var obj in detectedObjects)
        {
            var frameIndex = Math.Max(0, (int)Math.Floor(obj.AnalyzedFrame.TimeSeconds * fps));

            if (lastValidFrameIndex.HasValue)
            {
                frameIndex = Math.Min(frameIndex, lastValidFrameIndex.Value);
            }

            if (!analyzedFrames.TryGetValue(frameIndex, out var objects))
            {
                objects = new List<DetectedObject>();
                analyzedFrames[frameIndex] = objects;
            }

            objects.Add(obj);
        }

        foreach (var frameIndex in analyzedFrames.Keys.ToList())
        {
            analyzedFrames[frameIndex] = analyzedFrames[frameIndex]
                .GroupBy(o => o.Id)
                .Select(g => g.Last())
                .ToList();
        }

        return analyzedFrames;
    }

    private static List<DetectedObject> GetObjectsForFrame(
        Dictionary<int, List<DetectedObject>> analyzedFrames,
        int currentFrameIndex,
        int frameWidth,
        int frameHeight)
    {
        var sourceObjects = GetObjectsFromLatestAnalyzedFrame(analyzedFrames, currentFrameIndex);
        var result = new List<DetectedObject>();

        foreach (var obj in sourceObjects)
        {
            var rect = ClampRect(
                new Rect(obj.X, obj.Y, obj.Width, obj.Height),
                frameWidth,
                frameHeight);

            if (rect.Width > 0 && rect.Height > 0)
            {
                result.Add(new DetectedObject
                {
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height
                });
            }
        }

        return result;
    }

    private static List<DetectedObject> GetObjectsFromLatestAnalyzedFrame(
        Dictionary<int, List<DetectedObject>> analyzedFrames,
        int currentFrameIndex)
    {
        if (analyzedFrames.Count == 0)
            return [];

        List<DetectedObject>? activeObjects = null;

        foreach (var analyzedFrame in analyzedFrames.OrderBy(x => x.Key))
        {
            if (analyzedFrame.Key > currentFrameIndex)
                break;

            activeObjects = analyzedFrame.Value;
        }

        return activeObjects ?? [];
    }

    private static string BuildOutputPath(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? "";
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath);

        return Path.Combine(directory, $"{fileNameWithoutExtension}_anonymized{extension}");
    }

    private static int GetSafeFourCc(VideoCapture capture)
    {
        try
        {
            var originalFourCc = (int)capture.Get(VideoCaptureProperties.FourCC);
            if (originalFourCc != 0)
            {
                return originalFourCc;
            }
        }
        catch
        {
            // ignore and fall back
        }

        return FourCC.MP4V;
    }

    private static void BlurRegion(Mat frame, DetectedObject detectedObject)
    {
        const double scaleX = 1.25;
        const double scaleY = 1.40;
        const int marginX = 10;
        const int marginY = 20;

        int originalCenterX = detectedObject.X + detectedObject.Width / 2;
        int originalCenterY = detectedObject.Y + detectedObject.Height / 2;

        int expandedWidth = (int)(detectedObject.Width * scaleX) + marginX;
        int expandedHeight = (int)(detectedObject.Height * scaleY) + marginY;

        var rect = ClampRect(
            new Rect(
                originalCenterX - expandedWidth / 2,
                originalCenterY - expandedHeight / 2,
                expandedWidth,
                expandedHeight),
            frame.Width,
            frame.Height);

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var roi = new Mat(frame, rect);

        var blurWidth = MakeOdd(Math.Max(15, rect.Width / 3));
        var blurHeight = MakeOdd(Math.Max(15, rect.Height / 3));

        using var blurred = new Mat();
        Cv2.GaussianBlur(
            roi,
            blurred,
            new OpenCvSharp.Size(blurWidth, blurHeight),
            0);

        using var mask = Mat.Zeros(rect.Height, rect.Width, MatType.CV_8UC1).ToMat();

        var center = new Point(rect.Width / 2, rect.Height / 2);
        var axes = new OpenCvSharp.Size(rect.Width / 2, rect.Height / 2);

        Cv2.Ellipse(
            mask,
            center,
            axes,
            0,
            0,
            360,
            Scalar.White,
            -1);

        blurred.CopyTo(roi, mask);
    }

    private static Rect ClampRect(Rect rect, int maxWidth, int maxHeight)
    {
        var x = Math.Max(0, rect.X);
        var y = Math.Max(0, rect.Y);

        var right = Math.Min(maxWidth, rect.X + rect.Width);
        var bottom = Math.Min(maxHeight, rect.Y + rect.Height);

        var width = Math.Max(0, right - x);
        var height = Math.Max(0, bottom - y);

        return new Rect(x, y, width, height);
    }

    private static int MakeOdd(int value)
    {
        if (value < 1) return 1;
        return value % 2 == 0 ? value + 1 : value;
    }
}