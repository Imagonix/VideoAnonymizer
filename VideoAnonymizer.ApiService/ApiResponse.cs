namespace VideoAnonymizer.ApiService
{
    public class ApiResponse<T>
    {
        public T? Payload { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
}
