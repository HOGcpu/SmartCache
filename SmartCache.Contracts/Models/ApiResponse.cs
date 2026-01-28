public class ApiResponse<T>
{
    public string ApiVersion { get; set; } = "1.0";
    public int StatusCode { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
}
