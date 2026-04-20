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
            var tracks = GroupObjectsByTrack(selectedObjects, fps, totalFrames);

            using var frameMat = new Mat();
            var currentFrameIndex = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!capture.Read(frameMat) || frameMat.Empty())
                    break;

                var objectsToBlur = GetObjectsForFrame(tracks, currentFrameIndex, frameWidth, frameHeight);

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

        await messagePublisher.PublishAsync(RabbitMQConstants.RoutingKeys.Anonymized, new AnonymizedVideo(job.JobId, DateTime.Now), stoppingToken);
    }

    private static Dictionary<int, List<TrackedPosition>> GroupObjectsByTrack(
    List<DetectedObject> detectedObjects,
    double fps,
    int? totalFrames = null)
    {
        var tracks = new Dictionary<int, List<TrackedPosition>>();
        var lastValidFrameIndex = totalFrames.HasValue
            ? Math.Max(0, totalFrames.Value - 1)
            : (int?)null;

        foreach (var obj in detectedObjects)
        {
            if (obj.TrackId is null || obj.TrackId == 0)
                continue;

            if (!tracks.TryGetValue(obj.TrackId.Value, out var positions))
            {
                positions = new List<TrackedPosition>();
                tracks[obj.TrackId.Value] = positions;
            }

            var frameIndex = Math.Max(0, (int)Math.Floor(obj.AnalyzedFrame.TimeSeconds * fps));

            if (lastValidFrameIndex.HasValue)
            {
                frameIndex = Math.Min(frameIndex, lastValidFrameIndex.Value);
            }

            positions.Add(new TrackedPosition
            {
                FrameIndex = frameIndex,
                X = obj.X,
                Y = obj.Y,
                Width = obj.Width,
                Height = obj.Height
            });
        }

        foreach (var trackId in tracks.Keys.ToList())
        {
            var merged = tracks[trackId]
                .OrderBy(p => p.FrameIndex)
                .GroupBy(p => p.FrameIndex)
                .Select(g => g.Last())
                .ToList();

            tracks[trackId] = merged;
        }

        return tracks;
    }

    private static List<DetectedObject> GetObjectsForFrame(
        Dictionary<int, List<TrackedPosition>> tracks,
        int currentFrameIndex,
        int frameWidth,
        int frameHeight)
    {
        var result = new List<DetectedObject>();

        foreach (var track in tracks.Values)
        {
            var position = GetPositionForFrame(track, currentFrameIndex);
            if (position == null)
                continue;

            var rect = ClampRect(new Rect(position.X, position.Y,
                                          position.Width, position.Height),
                                 frameWidth, frameHeight);

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

    private static TrackedPosition? GetPositionForFrame(List<TrackedPosition> positions, int currentFrame)
    {
        if (positions.Count == 0)
            return null;

        TrackedPosition? active = null;

        foreach (var position in positions)
        {
            if (position.FrameIndex > currentFrame)
                break;

            active = position;
        }

        return active;
    }

    private class TrackedPosition
    {
        public int FrameIndex { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
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
        var rect = ClampRect(
            new Rect(
                detectedObject.X,
                detectedObject.Y,
                detectedObject.Width,
                detectedObject.Height),
            frame.Width,
            frame.Height);

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var roi = new Mat(frame, rect);

        var blurWidth = MakeOdd(Math.Max(15, rect.Width / 3));
        var blurHeight = MakeOdd(Math.Max(15, rect.Height / 3));

        Cv2.GaussianBlur(
            roi,
            roi,
            new OpenCvSharp.Size(blurWidth, blurHeight),
            0);
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