using System.Text.Json.Serialization;

namespace VideoAnonymizer.ObjectDetectionClient;

public partial class DetectionResult
{
    [JsonPropertyName("blurShape")]
    public string? BlurShape { get; set; }
}
