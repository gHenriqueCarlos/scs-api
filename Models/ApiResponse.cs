namespace ScspApi.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Code { get; set; }  
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T? data = default, string? message = null)
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string? message = null, IEnumerable<string>? errors = null, string? code = null)
        => new() { Success = false, Message = message, Errors = errors, Code = code };
}
