using System.Net.Http.Json;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Modules.Components;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class VideoEditorActionPersister(HttpClient client)
{
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

    public async Task ApplyUndoRedoAsync(
        VideoEditorAction action,
        bool isRedo,
        VideoEditor? videoEditor,
        Func<AnonymizationSettingsDto, Task> applySettings)
    {
        switch (action)
        {
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
