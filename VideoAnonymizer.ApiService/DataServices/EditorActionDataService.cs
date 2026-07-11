using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService.DataServices;

public sealed class EditorActionDataService(IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
{
    public async Task<EditorAction> AddActionAsync(Guid videoId, string actionType, string data)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var maxSeq = await db.EditorActions
            .Where(a => a.VideoId == videoId)
            .MaxAsync(a => (int?)a.SequenceNumber) ?? 0;

        var action = new EditorAction
        {
            VideoId = videoId,
            ActionType = actionType,
            SequenceNumber = maxSeq + 1,
            CreatedAt = DateTime.UtcNow,
            Undone = false,
            Data = data
        };

        db.EditorActions.Add(action);
        await db.SaveChangesAsync();
        return action;
    }

    public async Task<List<EditorAction>> GetActionsAsync(Guid videoId)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.EditorActions
            .Where(a => a.VideoId == videoId)
            .OrderBy(a => a.SequenceNumber)
            .ToListAsync();
    }

    public async Task UpdateActionDataAsync(Guid videoId, Guid actionId, string data)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var action = await db.EditorActions
            .FirstOrDefaultAsync(a => a.Id == actionId && a.VideoId == videoId);
        if (action is null)
            throw new NotFoundException();

        action.Data = data;
        await db.SaveChangesAsync();
    }

    public async Task ToggleUndoneAsync(Guid videoId, Guid actionId, bool undone)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var action = await db.EditorActions
            .FirstOrDefaultAsync(a => a.Id == actionId && a.VideoId == videoId);
        if (action is null)
            throw new NotFoundException();

        action.Undone = undone;
        await db.SaveChangesAsync();
    }
}
