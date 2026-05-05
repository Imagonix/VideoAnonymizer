using VideoAnonymizer.Web.Modules.Actions;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public static class VideoEditorActionDescriptions
{
    public static string Get(VideoEditorAction action) => action switch
    {
        ObjectAddedAction => "Added new bounding box",
        ObjectUpdatedAction a => a.OperationType switch
        {
            "toggle" => "Changed visibility",
            "reassign" => "Reassigned track ID",
            "move" => "Moved bounding box",
            "resize" => "Resized bounding box",
            _ => "Updated bounding box"
        },
        ObjectsBulkUpdatedAction a => a.OperationType switch
        {
            "merge" => $"Merged tracked Objects {string.Join(",", GetMergedTrackIdValues(a))}",
            "split" => $"Split out {a.Objects.Count} bounding boxes from track ID {a.BeforeState[0]?.TrackId}",
            "toggle" => $"Changed visibility ({a.Objects.Count} objects)",
            "reassign" => $"Reassigned track ID ({a.Objects.Count} objects)",
            _ => $"Updated {a.Objects.Count} objects"
        },
        ObjectDeletedAction => "Deleted bounding box",
        SettingsUpdatedAction a => GetSettingsDescription(a),
        _ => "Unknown action"
    };

    private static IEnumerable<int> GetMergedTrackIdValues(ObjectsBulkUpdatedAction action)
    {
        var targetId = action.Objects.FirstOrDefault()?.TrackId;
        var sourceIds = action.BeforeState
            .Select(o => o.TrackId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value);
        return targetId.HasValue
            ? new[] { targetId.Value }.Concat(sourceIds).Distinct()
            : sourceIds.Distinct();
    }

    private static string GetSettingsDescription(SettingsUpdatedAction action)
    {
        var before = action.BeforeState;
        var after = action.AfterState;

        if ((before.BlurSizePercent, before.TimeBufferMs) == (after.BlurSizePercent, after.TimeBufferMs))
            return "Saved settings";

        if (before.BlurSizePercent != after.BlurSizePercent && before.TimeBufferMs != after.TimeBufferMs)
            return $"Blur: {before.BlurSizePercent}% -> {after.BlurSizePercent}%, Buffer: {before.TimeBufferMs}ms -> {after.TimeBufferMs}ms";

        return before.BlurSizePercent != after.BlurSizePercent
            ? $"Blur: {before.BlurSizePercent}% -> {after.BlurSizePercent}%"
            : $"Buffer: {before.TimeBufferMs}ms -> {after.TimeBufferMs}ms";
    }
}
