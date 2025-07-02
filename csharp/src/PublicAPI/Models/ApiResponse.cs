using System.Text.Json.Serialization;

namespace Train.Solver.PublicAPI.Models;

public class ApiError
{
    //public string Code { get; set; } = null!;

    public string Message { get; set; } = null!;
}

public class ApiResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; set; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}