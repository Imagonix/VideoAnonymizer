using System.Net.Http.Json;
using System.Text.Json;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Modules.Components;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class VideoEditorActionPersister(HttpClient client)
{
    public async Task<Guid> RecordActionAsync(Guid videoId, VideoEditorAction action)
    {
        var (actionType, data) = SerializeAction(action);
        return await RecordRawActionAsync(videoId, actionType, data);
    }

    public async Task<Guid> RecordRawActionAsync(Guid videoId, string actionType, string data)
    {
        using var response = await client.PostAsJsonAsync(
            $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.Actions}",
            new RecordActionRequest { ActionType = actionType, Data = data });

        response.EnsureSuccessStatusCode();
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<EditorActionDto>>();
        return apiResponse?.Payload?.Id
            ?? throw new InvalidOperationException("Failed to record action.");
    }

    public async Task<List<EditorActionDto>> LoadActionHistoryAsync(Guid videoId)
    {
        var response = await client.GetAsync(
            $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.Actions}");
        response.EnsureSuccessStatusCode();
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<EditorActionDto>>>();
        return apiResponse?.Payload ?? [];
    }

    public async Task ToggleActionUndoneAsync(Guid videoId, Guid actionId, bool undone)
    {
        using var response = await client.PutAsJsonAsync(
            $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.Actions}/{actionId}/{SharedConstants.Paths.Undone}",
            new ToggleUndoneRequest { Undone = undone });
        response.EnsureSuccessStatusCode();
    }

    public async Task SaveAsync(VideoEditorAction action)
    {
        switch (action)
        {
            case ObjectAddedAction a:
                await EnsureSuccessfulResponseAsync(client.PostAsJsonAsync(
                    DetectedObjectRoute(a.VideoId, a.AnalyzedFrameId), a.Object));
                break;

            case ObjectUpdatedAction a:
                await EnsureSuccessfulResponseAsync(client.PutAsJsonAsync(
                    DetectedObjectRoute(a.VideoId, a.AnalyzedFrameId, a.Object.Id.ToString()), a.Object));
                break;

            case ObjectsBulkUpdatedAction a:
                await EnsureSuccessfulResponseAsync(client.PatchAsJsonAsync(
                    BulkDetectedObjectsRoute(a.VideoId), a.Objects));
                break;

            case ObjectDeletedAction a:
                await EnsureSuccessfulResponseAsync(client.DeleteAsync(
                    DetectedObjectRoute(a.VideoId, a.AnalyzedFrameId, a.Object.Id.ToString())));
                break;

            case SettingsUpdatedAction a:
                await SaveSettingsAsync(a.VideoId, a.AfterState);
                break;
        }
    }

    public async Task<TrackForwardJobDto> TrackForwardAsync(TrackForwardAction action, Guid jobId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/{SharedConstants.Paths.Analyzed}/{action.VideoId}/{SharedConstants.Paths.Tracks}/{SharedConstants.Paths.TrackForward}",
            new TrackForwardRequestDto
            {
                JobId = jobId,
                SeedDetectionId = action.Object.Id,
                BoundingBox = new TrackForwardBoundingBoxDto
                {
                    X = action.Object.X,
                    Y = action.Object.Y,
                    Width = action.Object.Width,
                    Height = action.Object.Height
                },
                ObjectClass = action.Object.ClassName,
                TrackId = action.Object.TrackId
            });

        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TrackForwardJobDto>>();
        return apiResponse?.Payload
            ?? throw new InvalidOperationException("Track forward response did not include a job id.");
    }

    public async Task ApplyUndoRedoAsync(
        VideoEditorAction action,
        bool isRedo,
        VideoEditor? videoEditor,
        Func<AnonymizationSettingsDto, Task> applySettings,
        ActionHistoryItem? historyItem = null)
    {
        switch (action)
        {
            case TrackForwardAction a when !isRedo && historyItem?.CreatedObjectIds.Count > 0:
                await EnsureSuccessfulResponseAsync(client.PostAsJsonAsync(
                    $"/{SharedConstants.Paths.Video}/{a.VideoId}/{SharedConstants.Paths.DetectedObjects}/delete",
                    new { ObjectIds = historyItem.CreatedObjectIds }));
                await PushChangesToVueAsync(videoEditor, [],
                    historyItem.CreatedObjectIds.Select(id => id.ToString()).ToList(), []);
                break;

            case TrackForwardAction a when isRedo && historyItem?.CreatedObjectDtos.Count > 0:
                foreach (var dto in historyItem.CreatedObjectDtos)
                {
                    await EnsureSuccessfulResponseAsync(client.PostAsJsonAsync(
                        DetectedObjectRoute(a.VideoId, dto.AnalyzedFrameId.ToString()), dto));
                }
                await PushChangesToVueAsync(videoEditor, [], [], historyItem.CreatedObjectDtos);
                break;

            case ObjectAddedAction a:
                if (isRedo)
                {
                    await EnsureSuccessfulResponseAsync(client.PostAsJsonAsync(
                        DetectedObjectRoute(a.VideoId, a.AnalyzedFrameId), a.Object));
                    await PushChangesToVueAsync(videoEditor, [], [], [a.Object]);
                }
                else
                {
                    await EnsureSuccessfulResponseAsync(client.DeleteAsync(
                        DetectedObjectRoute(a.VideoId, a.AnalyzedFrameId, a.Object.Id.ToString())));
                    await PushChangesToVueAsync(videoEditor, [], [a.Object.Id.ToString()], []);
                }
                break;

            case ObjectUpdatedAction a:
                IReadOnlyList<DetectedObjectDto> updateState = isRedo ? [a.Object] : a.BeforeState;
                if (updateState.Count > 0)
                {
                    var item = updateState[0];
                    await EnsureSuccessfulResponseAsync(client.PutAsJsonAsync(
                        DetectedObjectRoute(a.VideoId, item.AnalyzedFrameId.ToString(), item.Id.ToString()), item));
                    await PushChangesToVueAsync(videoEditor, updateState, [], []);
                }
                break;

            case ObjectsBulkUpdatedAction a:
                IReadOnlyList<DetectedObjectDto> bulkState = isRedo ? a.Objects : a.BeforeState;
                if (bulkState.Count > 0)
                {
                    await EnsureSuccessfulResponseAsync(client.PatchAsJsonAsync(
                        BulkDetectedObjectsRoute(a.VideoId), bulkState));
                    await PushChangesToVueAsync(videoEditor, bulkState, [], []);
                }
                break;

            case ObjectDeletedAction a:
                if (isRedo)
                {
                    await EnsureSuccessfulResponseAsync(client.DeleteAsync(
                        DetectedObjectRoute(a.VideoId, a.AnalyzedFrameId, a.Object.Id.ToString())));
                    await PushChangesToVueAsync(videoEditor, [], [a.Object.Id.ToString()], []);
                }
                else
                {
                    await EnsureSuccessfulResponseAsync(client.PostAsJsonAsync(
                        DetectedObjectRoute(a.VideoId, a.AnalyzedFrameId), a.Object));
                    await PushChangesToVueAsync(videoEditor, [], [], [a.Object]);
                }
                break;

            case SettingsUpdatedAction a:
                var settingsState = isRedo ? a.AfterState : a.BeforeState;
                await SaveSettingsAsync(a.VideoId, settingsState);
                await applySettings(settingsState);
                break;
        }
    }

    private static (string ActionType, string Data) SerializeAction(VideoEditorAction action)
    {
        return action switch
        {
            ObjectAddedAction a => ("add", JsonSerializer.Serialize(new ActionDataAdd(a.AnalyzedFrameId, a.Object))),
            ObjectUpdatedAction a => ("update", JsonSerializer.Serialize(new ActionDataUpdate(a.AnalyzedFrameId, a.BeforeState[0], a.Object, a.OperationType))),
            ObjectsBulkUpdatedAction a => ("bulk-update", JsonSerializer.Serialize(new ActionDataBulkUpdate([.. a.BeforeState], [.. a.Objects], a.OperationType))),
            ObjectDeletedAction a => ("delete", JsonSerializer.Serialize(new ActionDataDelete(a.AnalyzedFrameId, a.Object))),
            SettingsUpdatedAction a => ("settings", JsonSerializer.Serialize(new ActionDataSettings(a.BeforeState, a.AfterState))),
            _ => throw new ArgumentException($"Unsupported action type: {action.GetType().Name}")
        };
    }

    private Task SaveSettingsAsync(Guid videoId, AnonymizationSettingsDto settings) =>
        EnsureSuccessfulResponseAsync(client.PutAsJsonAsync(
            $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.VideoSettings}",
            settings));

    private static async Task PushChangesToVueAsync(
        VideoEditor? videoEditor,
        IReadOnlyList<DetectedObjectDto> objectsToUpdate,
        IReadOnlyList<string> objectsToRemove,
        IReadOnlyList<DetectedObjectDto> objectsToAdd)
    {
        if (videoEditor is null)
            return;

        await videoEditor.PushChangesToVue(new DetectedObjectChangeSet
        {
            ObjectsToUpdate = objectsToUpdate,
            ObjectsToRemove = objectsToRemove,
            ObjectsToAdd = objectsToAdd
        });
    }

    private static async Task EnsureSuccessfulResponseAsync(Task<HttpResponseMessage> requestTask)
    {
        using var response = await requestTask;
        response.EnsureSuccessStatusCode();
    }

    private static string DetectedObjectRoute(string videoId, string analyzedFrameId, string? objectId = null)
    {
        var route = $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.AnalyzedFrame}/{analyzedFrameId}/{SharedConstants.Paths.DetectedObject}";
        return objectId is null ? route : $"{route}/{objectId}";
    }

    private static string BulkDetectedObjectsRoute(string videoId) =>
        $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.DetectedObjects}";
}
