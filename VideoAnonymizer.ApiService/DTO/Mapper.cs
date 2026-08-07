using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared.DTO;

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
                FrameIndex = entity.FrameIndex,
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
                BlurShape = entity.BlurShape,
                BlurSizePercentOverride = entity.BlurSizePercentOverride,
                OccurrenceBlurSizePercentOverride = entity.OccurrenceBlurSizePercentOverride,
                PreBufferMsOverride = entity.PreBufferMsOverride,
                PostBufferMsOverride = entity.PostBufferMsOverride,
                NextGapHandlingMode = ToWireGapHandlingMode(entity.NextGapHandlingMode),
                Selected = entity.Selected,
                TrackId = entity.TrackId,
                X = entity.X,
                Y = entity.Y,
                Width = entity.Width,
                Height = entity.Height,
                AnalyzedFrameId = entity.AnalyzedFrameId
            };
        }

        public static EditorActionDto ToDto(this EditorAction entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new EditorActionDto
            {
                Id = entity.Id,
                VideoId = entity.VideoId,
                ActionType = entity.ActionType,
                Data = entity.Data,
                Undone = entity.Undone,
                SequenceNumber = entity.SequenceNumber,
                CreatedAt = entity.CreatedAt
            };
        }

        public static AnalyzedFrame ToEntity(this AnalyzedFrameDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var entity = new AnalyzedFrame
            {
                Id = dto.Id,
                FrameIndex = dto.FrameIndex,
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
                BlurShape = dto.BlurShape,
                BlurSizePercentOverride = dto.BlurSizePercentOverride,
                OccurrenceBlurSizePercentOverride = dto.OccurrenceBlurSizePercentOverride,
                PreBufferMsOverride = dto.PreBufferMsOverride,
                PostBufferMsOverride = dto.PostBufferMsOverride,
                NextGapHandlingMode = FromWireGapHandlingMode(dto.NextGapHandlingMode),
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

        public static List<EditorActionDto> ToDtos(this IEnumerable<EditorAction> entities)
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
            entity.FrameIndex = dto.FrameIndex;
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
            entity.BlurShape = dto.BlurShape;
            entity.BlurSizePercentOverride = dto.BlurSizePercentOverride;
            entity.OccurrenceBlurSizePercentOverride = dto.OccurrenceBlurSizePercentOverride;
            entity.PreBufferMsOverride = dto.PreBufferMsOverride;
            entity.PostBufferMsOverride = dto.PostBufferMsOverride;
            entity.NextGapHandlingMode = FromWireGapHandlingMode(dto.NextGapHandlingMode);
            entity.Selected = dto.Selected;
            entity.TrackId = dto.TrackId;
            entity.X = dto.X;
            entity.Y = dto.Y;
            entity.Width = dto.Width;
            entity.Height = dto.Height;
            entity.AnalyzedFrameId = dto.AnalyzedFrameId;
        }

        /// <summary>
        /// Entity enum to the exact API wire name. Null remains null.
        /// </summary>
        public static string? ToWireGapHandlingMode(GapHandlingMode? mode) =>
            mode switch
            {
                null => null,
                GapHandlingMode.Interpolate => nameof(GapHandlingMode.Interpolate),
                GapHandlingMode.UseBuffers => nameof(GapHandlingMode.UseBuffers),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    $"Unsupported gap handling mode: {mode}")
            };

        /// <summary>
        /// Exact wire name to entity enum. Null remains null. Unsupported non-null
        /// values are rejected rather than silently treated as Interpolate.
        /// </summary>
        public static GapHandlingMode? FromWireGapHandlingMode(string? wireValue)
        {
            if (wireValue is null)
                return null;

            if (wireValue == nameof(GapHandlingMode.Interpolate))
                return GapHandlingMode.Interpolate;

            if (wireValue == nameof(GapHandlingMode.UseBuffers))
                return GapHandlingMode.UseBuffers;

            throw new ArgumentException(
                $"Unsupported gap handling mode: '{wireValue}'. Expected 'Interpolate', 'UseBuffers', or null.",
                nameof(wireValue));
        }
    }
}
