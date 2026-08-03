namespace VideoAnonymizer.Web.Shared.DTO
{
    public class VideoDto
    {
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public int BlurSizePercent { get; set; } = 120;
        public int TimeBufferMs { get; set; } = 300;

        /// <summary>
        /// True when the video has at least one analyzed frame.
        /// </summary>
        public bool HasAnalysis { get; set; }

        /// <summary>
        /// True when an anonymized output path is recorded for the video.
        /// </summary>
        public bool HasAnonymizedOutput { get; set; }

        /// <summary>
        /// Compact display status derived from analysis and export state.
        /// </summary>
        public string Status { get; set; } = VideoListStatuses.Imported;
    }

    /// <summary>
    /// Compact statuses shown in the imported-video table.
    /// </summary>
    public static class VideoListStatuses
    {
        public const string Imported = "Imported";
        public const string ReadyToReview = "Ready to review";
        public const string Exported = "Exported";

        public static string FromFlags(bool hasAnalysis, bool hasAnonymizedOutput)
        {
            if (hasAnonymizedOutput)
                return Exported;
            if (hasAnalysis)
                return ReadyToReview;
            return Imported;
        }
    }
}
