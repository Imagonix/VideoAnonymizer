using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Actions;

public sealed record SettingsUpdatedAction : VideoEditorAction
{
    public required Guid VideoId { get; init; }
    public required AnonymizationSettingsDto BeforeState { get; init; }
    public required AnonymizationSettingsDto AfterState { get; init; }
}
