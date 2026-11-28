namespace Warranty.Services;

public enum ServiceErrorCode
{
    NotFound,
    Forbidden,
    Invalid
}

public sealed record ServiceResult<T>(T? Value, ServiceErrorCode? ErrorCode, string? ErrorMessage)
{
    public bool IsSuccess => ErrorCode is null;

    public static ServiceResult<T> Success(T value) => new(value, null, null);

    public static ServiceResult<T> Failure(ServiceErrorCode errorCode, string errorMessage) =>
        new(default, errorCode, errorMessage);
}
