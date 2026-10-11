namespace LMS.API.Common;

public enum ServiceErrorType
{
    None,
    Validation,
    Conflict,
    Unauthorized,
    Forbidden,
    NotFound
}

public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public ServiceErrorType ErrorType { get; private set; } = ServiceErrorType.None;

    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T> { Success = true, Data = data };
    }

    public static ServiceResult<T> Fail(string error, ServiceErrorType errorType)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Error = error,
            ErrorType = errorType
        };
    }
}