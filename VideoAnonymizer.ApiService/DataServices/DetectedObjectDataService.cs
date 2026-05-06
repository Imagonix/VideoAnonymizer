using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.DataServices;

public class DetectedObjectDataService(IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
{
    public async Task<DetectedObjectDto> AddDetectedObject(Guid videoId, DetectedObjectDto dto)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var frame = await db.AnalyzedFrames
            .FirstOrDefaultAsync(f => f.Id == dto.AnalyzedFrameId && f.VideoId == videoId);
        if (frame == null)
            throw new NotFoundException();

        var entity = Mapper.ToEntity(dto);
        db.DetectedObjects.Add(entity);
        await db.SaveChangesAsync();

        return Mapper.ToDto(entity);
    }

    public async Task<DetectedObjectDto> UpdateDetectedObject(Guid videoId, DetectedObjectDto dto)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.DetectedObjects
            .Include(o => o.AnalyzedFrame)
            .FirstOrDefaultAsync(o => o.Id == dto.Id
                && o.AnalyzedFrameId == dto.AnalyzedFrameId
                && o.AnalyzedFrame.VideoId == videoId);
        if (entity == null)
            throw new NotFoundException();

        Mapper.UpdateEntity(dto, entity);
        await db.SaveChangesAsync();

        return Mapper.ToDto(entity);
    }

    public async Task DeleteDetectedObject(Guid videoId, Guid analyzedFrameId, Guid objectId)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.DetectedObjects
            .Include(o => o.AnalyzedFrame)
            .FirstOrDefaultAsync(o => o.Id == objectId
                && o.AnalyzedFrameId == analyzedFrameId
                && o.AnalyzedFrame.VideoId == videoId);
        if (entity == null)
            throw new NotFoundException();

        db.DetectedObjects.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task BulkUpdateDetectedObjects(Guid videoId, IReadOnlyList<DetectedObjectDto> dtos)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var objectIds = dtos.Select(d => d.Id).ToHashSet();

        var existing = await db.DetectedObjects
            .Include(o => o.AnalyzedFrame)
            .Where(o => objectIds.Contains(o.Id) && o.AnalyzedFrame.VideoId == videoId)
            .ToListAsync();

        if (existing.Count != dtos.Count)
            throw new NotFoundException();

        var dtoMap = dtos.ToDictionary(d => d.Id);
        foreach (var entity in existing)
        {
            var dto = dtoMap[entity.Id];
            if (dto.AnalyzedFrameId != entity.AnalyzedFrameId)
                throw new NotFoundException();

            Mapper.UpdateEntity(dto, entity);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
