using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor;

public class VideoAnonomyzer(
    ILogger<VideoAnonomyzer> logger,
    IServiceProvider serviceProvider)
    : SingleJobQueingWorker<AnonomyzeVideo>(logger)
{
    protected override async Task HandleJob(AnonomyzeVideo job, CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<VideoAnonymizerDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var video = await db.Videos.FindAsync(job.videoId, stoppingToken);

        if (video is null)
        {
            throw new InvalidOperationException(
                $"video '{job.videoId}' not found");
        }

        var selectedFrames = await db.AnalyzedFrames
            .AsNoTracking()
            .Where(f => f.VideoId == video.Id)
            .Include(f => f.DetectedObjects.Where(o => o.Selected))
            .Where(f => f.DetectedObjects.Any(o => o.Selected))
            .OrderBy(f => f.TimeSeconds)
            .ToListAsync(stoppingToken);

        if (!File.Exists(video.SourcePath))
        {
            throw new FileNotFoundException(
                $"Source video file does not exist: {video.SourcePath}",
                video.SourcePath);
        }

        var sourcePath = video.SourcePath;
        var outputPath = BuildOutputPath(sourcePath);

        using var capture = new VideoCapture(sourcePath);
        if (!capture.IsOpened())
        {
            throw new InvalidOperationException($"Could not open source video '{sourcePath}'.");
        }

        var fps = capture.Fps;
        if (fps <= 0)
        {
            fps = 25; // fallback
        }

        var frameWidth = capture.FrameWidth;
        var frameHeight = capture.FrameHeight;

        if (frameWidth <= 0 || frameHeight <= 0)
        {
            throw new InvalidOperationException(
                $"Invalid video dimensions detected for '{sourcePath}'. Width={frameWidth}, Height={frameHeight}");
        }

        var fourCc = GetSafeFourCc(capture);
        using var writer = new VideoWriter(
            outputPath,
            fourCc,
            fps,
            new OpenCvSharp.Size(frameWidth, frameHeight));

        if (!writer.IsOpened())
        {
            throw new InvalidOperationException($"Could not create output video '{outputPath}'.");
        }

        var analyzedFramesByIndex = selectedFrames
             .Select(frame => new
             {
                 FrameIndex = Math.Max(0, (int)Math.Round(frame.TimeSeconds * fps)),
                 Objects = frame.DetectedObjects.ToList()
             })
             .OrderBy(x => x.FrameIndex)
             .ToList();

        using var frameMat = new Mat();

        var currentFrameIndex = 0;
        var nearestPointer = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!capture.Read(frameMat) || frameMat.Empty())
            {
                break;
            }

            if (analyzedFramesByIndex.Count > 0)
            {
                // Pointer nach vorne schieben, solange der nächste analysierte Frame
                // näher oder gleich nah am aktuellen Frame ist.
                while (nearestPointer < analyzedFramesByIndex.Count - 1)
                {
                    var currentDistance = Math.Abs(analyzedFramesByIndex[nearestPointer].FrameIndex - currentFrameIndex);
                    var nextDistance = Math.Abs(analyzedFramesByIndex[nearestPointer + 1].FrameIndex - currentFrameIndex);

                    if (nextDistance <= currentDistance)
                    {
                        nearestPointer++;
                    }
                    else
                    {
                        break;
                    }
                }

                var objectsToBlur = analyzedFramesByIndex[nearestPointer].Objects;

                foreach (var detectedObject in objectsToBlur)
                {
                    BlurRegion(frameMat, detectedObject);
                }
            }

            writer.Write(frameMat);
            currentFrameIndex++;
        }

        video.AnonomizedPath = outputPath;
        await db.SaveChangesAsync(stoppingToken);

        await publishEndpoint.Publish(
            new AnonomyzedVideo(job.jobId, DateTime.Now),
            stoppingToken);
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

        // mp4v is a pragmatic fallback for mp4 output
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

        // Kernel size must be odd and > 0.
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