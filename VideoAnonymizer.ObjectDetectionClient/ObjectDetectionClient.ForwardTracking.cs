namespace VideoAnonymizer.ObjectDetectionClient;

public partial class ObjectDetectionClient
{
    public virtual System.Threading.Tasks.Task<TrackForwardPythonResponse> TrackForwardAsync(TrackForwardPythonRequest body)
    {
        return TrackForwardAsync(body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task<TrackForwardPythonResponse> TrackForwardAsync(
        TrackForwardPythonRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        if (body == null)
            throw new System.ArgumentNullException(nameof(body));

        var client_ = _httpClient;

        using var request_ = new System.Net.Http.HttpRequestMessage();
        var json_ = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body, JsonSerializerSettings);
        var content_ = new System.Net.Http.ByteArrayContent(json_);
        content_.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
        request_.Content = content_;
        request_.Method = new System.Net.Http.HttpMethod("POST");
        request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl))
            urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("trackForward");

        PrepareRequest(client_, request_, urlBuilder_);

        var url_ = urlBuilder_.ToString();
        request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);

        PrepareRequest(client_, request_, url_);

        using var response_ = await client_.SendAsync(
                request_,
                System.Net.Http.HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);

        var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
        foreach (var item_ in response_.Headers)
            headers_[item_.Key] = item_.Value;
        if (response_.Content?.Headers != null)
        {
            foreach (var item_ in response_.Content.Headers)
                headers_[item_.Key] = item_.Value;
        }

        ProcessResponse(client_, response_);

        var status_ = (int)response_.StatusCode;
        if (status_ == 200)
        {
            var objectResponse_ = await ReadObjectResponseAsync<TrackForwardPythonResponse>(
                    response_,
                    headers_,
                    cancellationToken)
                .ConfigureAwait(false);

            if (objectResponse_.Object == null)
            {
                throw new ApiException(
                    "Response was null which was not expected.",
                    status_,
                    objectResponse_.Text,
                    headers_,
                    null!);
            }

            return objectResponse_.Object;
        }

        if (status_ == 422)
        {
            var objectResponse_ = await ReadObjectResponseAsync<HTTPValidationError>(
                    response_,
                    headers_,
                    cancellationToken)
                .ConfigureAwait(false);

            if (objectResponse_.Object == null)
            {
                throw new ApiException(
                    "Response was null which was not expected.",
                    status_,
                    objectResponse_.Text,
                    headers_,
                    null!);
            }

            throw new ApiException<HTTPValidationError>(
                "Validation Error",
                status_,
                objectResponse_.Text,
                headers_,
                objectResponse_.Object,
                null!);
        }

        var responseData_ = response_.Content == null
            ? null
            : await response_.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new ApiException(
            "The HTTP status code of the response was not expected (" + status_ + ").",
            status_,
            responseData_!,
            headers_,
            null!);
    }
}

public sealed class TrackForwardPythonRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("videoPath")]
    public string VideoPath { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("seedFrameIndex")]
    public int? SeedFrameIndex { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("seedTimeMs")]
    public int SeedTimeMs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("boundingBox")]
    public TrackForwardPythonBoundingBox BoundingBox { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("objectClass")]
    public string ObjectClass { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("trackId")]
    public int TrackId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("persistEveryMs")]
    public int PersistEveryMs { get; set; } = 100;

    [System.Text.Json.Serialization.JsonPropertyName("persistFrameIndexes")]
    public System.Collections.Generic.ICollection<int> PersistFrameIndexes { get; set; } =
        new System.Collections.ObjectModel.Collection<int>();

    [System.Text.Json.Serialization.JsonPropertyName("maxLostDurationMs")]
    public int MaxLostDurationMs { get; set; } = 5000;

    [System.Text.Json.Serialization.JsonPropertyName("recoveryDetectorIntervalMs")]
    public int RecoveryDetectorIntervalMs { get; set; } = 250;

    [System.Text.Json.Serialization.JsonPropertyName("trackerType")]
    public string TrackerType { get; set; } = "CSRT";

    [System.Text.Json.Serialization.JsonPropertyName("searchAreaExpansion")]
    public double SearchAreaExpansion { get; set; } = 3.0;

    [System.Text.Json.Serialization.JsonPropertyName("maxTrackDurationMs")]
    public int MaxTrackDurationMs { get; set; } = 30000;
}

public sealed class TrackForwardPythonBoundingBox
{
    [System.Text.Json.Serialization.JsonPropertyName("x")]
    public int X { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("y")]
    public int Y { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int Width { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int Height { get; set; }
}

public sealed class TrackForwardPythonResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("trackId")]
    public int TrackId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("detections")]
    public System.Collections.Generic.ICollection<TrackForwardPythonDetectionResult> Detections { get; set; } =
        new System.Collections.ObjectModel.Collection<TrackForwardPythonDetectionResult>();

    [System.Text.Json.Serialization.JsonPropertyName("reacquiredCount")]
    public int ReacquiredCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("gaps")]
    public System.Collections.Generic.ICollection<TrackForwardPythonGap> Gaps { get; set; } =
        new System.Collections.ObjectModel.Collection<TrackForwardPythonGap>();

    [System.Text.Json.Serialization.JsonPropertyName("stoppedReason")]
    public string StoppedReason { get; set; } = string.Empty;
}

public sealed class TrackForwardPythonDetectionResult
{
    [System.Text.Json.Serialization.JsonPropertyName("frameIndex")]
    public int FrameIndex { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("timeMs")]
    public int TimeMs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("className")]
    public string ClassName { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("x")]
    public int X { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("y")]
    public int Y { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int Width { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int Height { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("blurShape")]
    public string? BlurShape { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("trackId")]
    public int TrackId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("reacquired")]
    public bool Reacquired { get; set; }
}

public sealed class TrackForwardPythonGap
{
    [System.Text.Json.Serialization.JsonPropertyName("startTimeMs")]
    public int StartTimeMs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("endTimeMs")]
    public int EndTimeMs { get; set; }
}
