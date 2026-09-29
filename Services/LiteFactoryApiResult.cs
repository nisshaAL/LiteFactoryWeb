namespace LiteFactoryWeb.Services;

public sealed class LiteFactoryApiResult<T>
{
    private LiteFactoryApiResult(bool success, T? value, string? errorMessage)
    {
        Success = success;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public T? Value { get; }

    public string? ErrorMessage { get; }

    public static LiteFactoryApiResult<T> Ok(T value)
    {
        return new LiteFactoryApiResult<T>(true, value, null);
    }

    public static LiteFactoryApiResult<T> Error(string message)
    {
        return new LiteFactoryApiResult<T>(false, default, message);
    }
}
