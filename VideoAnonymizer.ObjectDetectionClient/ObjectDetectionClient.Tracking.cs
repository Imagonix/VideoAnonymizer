namespace VideoAnonymizer.ObjectDetectionClient;

public partial class ObjectDetectionClient
{
    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<DetectionResult>> TrackObjectsAsync(TrackObjectsRequest body)
    {
        return TrackObjectsAsync(body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task<System.Collections.Generic.ICollection<DetectionResult>> TrackObjectsAsync(
        TrackObjectsRequest body,
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
        urlBuilder_.Append("trackObjects");

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
            var objectResponse_ = await ReadObjectResponseAsync<System.Collections.Generic.ICollection<DetectionResult>>(
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

public sealed class TrackObjectsRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("detections")]
    public System.Collections.Generic.ICollection<DetectionResult> Detections { get; set; } =
        new System.Collections.ObjectModel.Collection<DetectionResult>();

    [System.Text.Json.Serialization.JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("fps")]
    public double Fps { get; set; } = 25D;
}
