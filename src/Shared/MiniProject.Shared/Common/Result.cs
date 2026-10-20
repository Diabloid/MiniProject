using System.Net;

namespace MiniProject.Shared.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public HttpStatusCode StatusCode { get; }

    protected Result(bool isSuccess, string? errorMessage, HttpStatusCode statusCode)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
    }

    public static Result Success(HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(true, null, statusCode);

    public static Result Failure(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
        new(false, errorMessage, statusCode);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? errorMessage, HttpStatusCode statusCode)
        : base(isSuccess, errorMessage, statusCode)
    {
        Value = value;
    }

    public static Result<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(true, value, null, statusCode);

    public static new Result<T> Failure(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
        new(false, default, errorMessage, statusCode);
}
