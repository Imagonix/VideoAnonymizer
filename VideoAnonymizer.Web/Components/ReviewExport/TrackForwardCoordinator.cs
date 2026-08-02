using System.Text.Json;
using MudBlazor;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Modules.Components;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class TrackForwardCoordinator(
    IHttpClientFactory httpClientFactory,
    ISnackbar snackbar,
    Func<Guid?> getVideoId,
    Func<VideoEditor?> getVideoEditor,
    Func<string, ActionHistoryItem> addAction,
    Action<VideoEditorAction, ActionHistoryItem, Guid> addUndoRedoAction,
    Func<Task> stateChanged,
    Func<Task> trackingCompleted)
{
    private readonly Dictionary<Guid, TrackingJob> _pendingJobs = [];

    public int ActiveCount { get; private set; }
    public bool HasErrors { get; private set; }

    public async Task StartAsync(TrackForwardAction action)
    {
        var item = addAction(VideoEditorActionDescriptions.Get(action));
        var jobId = Guid.NewGuid();
        var seedObjectId = action.Object.Id.ToString();

        // Pre-populate so early progress messages are not dropped.
        _pendingJobs[jobId] = new TrackingJob(item, seedObjectId, action);
        ActiveCount++;

        try
        {
            using var client = httpClientFactory.CreateClient("ApiService");
            var persister = new VideoEditorActionPersister(client);
            var job = await persister.TrackForwardAsync(action, jobId);
            if (job.JobId != jobId)
            {
                var pendingJob = _pendingJobs[jobId];
                _pendingJobs.Remove(jobId);
                _pendingJobs[job.JobId] = pendingJob;
            }

            await stateChanged();
        }
        catch
        {
            _pendingJobs.Remove(jobId);
            ActiveCount = Math.Max(0, ActiveCount - 1);
            item.Status = ActionStatus.Failed;
            HasErrors = true;
            snackbar.Add("Failed to save changes. Please refresh the page.", Severity.Error);
            await stateChanged();
        }
    }

    public async Task HandleProgressAsync(TrackForwardProgressMessage message)
    {
        if (!_pendingJobs.TryGetValue(message.JobId, out var trackingJob)
            || !IsForCurrentVideo(message.VideoId))
        {
            return;
        }

        if (!string.Equals(message.Status, SharedConstants.SignalR.Status.Completed, StringComparison.OrdinalIgnoreCase))
        {
            trackingJob.Item.Status = ActionStatus.Failed;
            HasErrors = true;
            if (getVideoEditor() is { } failedEditor)
            {
                await failedEditor.ClearTrackingObjectId(trackingJob.SeedObjectId);
                await failedEditor.PushTrackingProgress(message.TrackId ?? trackingJob.Action.Object.TrackId, 0, 0);
            }

            await stateChanged();
            return;
        }

        trackingJob.AddCreatedObjects(message.CreatedObjects);

        if (message.CreatedObjects.Count > 0 && getVideoEditor() is { } editor)
        {
            await editor.PushChangesToVue(new DetectedObjectChangeSet
            {
                ObjectsToUpdate = [],
                ObjectsToRemove = [],
                ObjectsToAdd = message.CreatedObjects
            });
        }

        if (getVideoEditor() is { } progressEditor)
        {
            await progressEditor.PushTrackingProgress(
                message.TrackId ?? trackingJob.Action.Object.TrackId,
                message.IsFinal ? 0 : message.GapStartTimeMs,
                message.IsFinal ? 0 : message.GapEndTimeMs);
        }
    }

    public async Task HandleCompletedAsync(TrackForwardCompletedMessage message)
    {
        if (!_pendingJobs.TryGetValue(message.JobId, out var trackingJob)
            || !IsForCurrentVideo(message.VideoId))
        {
            return;
        }

        _pendingJobs.Remove(message.JobId);
        ActiveCount = Math.Max(0, ActiveCount - 1);
        trackingJob.AddCreatedObjects(message.CreatedObjects);
        var createdObjects = trackingJob.CreatedObjects.Values.ToList();

        var completedSuccessfully =
            string.Equals(message.Status, SharedConstants.SignalR.Status.Completed, StringComparison.OrdinalIgnoreCase)
            && message.Result is not null;
        var retainedPartialResults =
            !completedSuccessfully
            && message.Result is not null
            && createdObjects.Count > 0;

        if (!completedSuccessfully && !retainedPartialResults)
        {
            trackingJob.Item.Status = ActionStatus.Failed;
            HasErrors = true;
            snackbar.Add(
                string.IsNullOrWhiteSpace(message.Error)
                    ? "Tracking failed."
                    : $"Tracking failed: {message.Error}",
                Severity.Error);

            if (getVideoEditor() is { } failedEditor)
            {
                await failedEditor.ClearTrackingObjectId(trackingJob.SeedObjectId);
                await failedEditor.PushTrackingProgress(message.Result?.TrackId ?? trackingJob.Action.Object.TrackId, 0, 0);
            }

            await stateChanged();
            return;
        }

        trackingJob.Item.Status = retainedPartialResults ? ActionStatus.Partial : ActionStatus.Success;
        trackingJob.Item.CreatedObjectIds = createdObjects.Select(o => o.Id).ToList();
        trackingJob.Item.CreatedObjectDtos = createdObjects;

        var trackForwardData = JsonSerializer.Serialize(new ActionDataTrackForward(
            trackingJob.Action.AnalyzedFrameId,
            trackingJob.Action.Object,
            createdObjects,
            message.Result!.TrackId,
            retainedPartialResults));
        using var recordClient = httpClientFactory.CreateClient("ApiService");
        var recordPersister = new VideoEditorActionPersister(recordClient);
        var actionId = await recordPersister.RecordRawActionAsync(message.VideoId, "track-forward", trackForwardData);
        addUndoRedoAction(trackingJob.Action, trackingJob.Item, actionId);

        if (getVideoEditor() is { } completedEditor)
        {
            await completedEditor.ClearTrackingObjectId(trackingJob.SeedObjectId);
            await completedEditor.PushTrackingProgress(message.Result.TrackId, 0, 0);
        }

        if (retainedPartialResults)
        {
            HasErrors = true;
            var matchLabel = createdObjects.Count == 1 ? "match" : "matches";
            snackbar.Add(
                $"Tracking stopped unexpectedly. {createdObjects.Count} {matchLabel} found so far were kept. "
                + "You can continue tracking from the last occurrence.",
                Severity.Warning);
        }

        await trackingCompleted();
        await stateChanged();
    }

    public void Dispose() => _pendingJobs.Clear();

    private bool IsForCurrentVideo(Guid videoId) =>
        !getVideoId().HasValue || getVideoId() == videoId;

    private sealed class TrackingJob(ActionHistoryItem item, string seedObjectId, TrackForwardAction action)
    {
        public ActionHistoryItem Item { get; } = item;
        public string SeedObjectId { get; } = seedObjectId;
        public TrackForwardAction Action { get; } = action;
        public Dictionary<Guid, DetectedObjectDto> CreatedObjects { get; } = [];

        public void AddCreatedObjects(IEnumerable<DetectedObjectDto> objects)
        {
            foreach (var detectedObject in objects)
                CreatedObjects[detectedObject.Id] = detectedObject;
        }
    }
}
