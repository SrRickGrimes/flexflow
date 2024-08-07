namespace FlexFlow;

public abstract class Result(bool isSuccess, string error)
{
    public bool IsSuccess { get; } = isSuccess;

    public string Error { get; } = error;
}

public class Result<T>(T value, bool isSuccess, string error) : Result(isSuccess, error)
{
    public T Value { get; } = value;

    public static Result<T> Success(T value) => new(value, true, default!);

    public static Result<T> Failure(string error) => new(default!, false, error);
}
