using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService.DTO
{
    public static class Mapper
    {
        public static AnalyzedFrameDto ToDto(this AnalyzedFrame entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new AnalyzedFrameDto
            {
                Id = entity.Id,
                TimeSeconds = entity.TimeSeconds,
                VideoId = entity.VideoId,
                DetectedObjects = entity.DetectedObjects?
                    .Select(x => x.ToDto())
                    .ToList()
                    ?? []
            };
        }

        public static DetectedObjectDto ToDto(this DetectedObject entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new DetectedObjectDto
            {
                Id = entity.Id,
                Confidence = entity.Confidence,
                ClassName = entity.ClassName,
                Selected = entity.Selected,
                TrackId = entity.TrackId,
                X = entity.X,
                Y = entity.Y,
                Width = entity.Width,
                Height = entity.Height,
                AnalyzedFrameId = entity.AnalyzedFrameId
            };
        }

        public static AnalyzedFrame ToEntity(this AnalyzedFrameDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var entity = new AnalyzedFrame
            {
                Id = dto.Id,
                TimeSeconds = dto.TimeSeconds,
                VideoId = dto.VideoId,
                DetectedObjects = dto.DetectedObjects?
                    .Select(x => x.ToEntity())
                    .ToList()
                    ?? []
            };

            foreach (var detectedObject in entity.DetectedObjects)
            {
                detectedObject.AnalyzedFrameId = entity.Id;
            }

            return entity;
        }

        public static DetectedObject ToEntity(this DetectedObjectDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new DetectedObject
            {
                Id = dto.Id,
                Confidence = dto.Confidence,
                ClassName = dto.ClassName,
                Selected = dto.Selected,
                TrackId = dto.TrackId,
                X = dto.X,
                Y = dto.Y,
                Width = dto.Width,
                Height = dto.Height,
                AnalyzedFrameId = dto.AnalyzedFrameId
            };
        }

        public static List<AnalyzedFrameDto> ToDtos(this IEnumerable<AnalyzedFrame> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);
            return entities.Select(x => x.ToDto()).ToList();
        }

        public static List<DetectedObjectDto> ToDtos(this IEnumerable<DetectedObject> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);
            return entities.Select(x => x.ToDto()).ToList();
        }

        public static List<AnalyzedFrame> ToEntities(this IEnumerable<AnalyzedFrameDto> dtos)
        {
            ArgumentNullException.ThrowIfNull(dtos);
            return dtos.Select(x => x.ToEntity()).ToList();
        }

        public static List<DetectedObject> ToEntities(this IEnumerable<DetectedObjectDto> dtos)
        {
            ArgumentNullException.ThrowIfNull(dtos);
            return dtos.Select(x => x.ToEntity()).ToList();
        }

        public static void UpdateEntity(this AnalyzedFrameDto dto, AnalyzedFrame entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            entity.TimeSeconds = dto.TimeSeconds;
            entity.VideoId = dto.VideoId;

            entity.DetectedObjects = dto.DetectedObjects?
                .Select(x =>
                {
                    var detectedObject = x.ToEntity();
                    detectedObject.AnalyzedFrameId = entity.Id;
                    return detectedObject;
                })
                .ToList()
                ?? [];
        }

        public static void UpdateEntity(this DetectedObjectDto dto, DetectedObject entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            entity.Confidence = dto.Confidence;
            entity.ClassName = dto.ClassName;
            entity.Selected = dto.Selected;
            entity.TrackId = dto.TrackId;
            entity.X = dto.X;
            entity.Y = dto.Y;
            entity.Width = dto.Width;
            entity.Height = dto.Height;
            entity.AnalyzedFrameId = dto.AnalyzedFrameId;
        }
    }
}
