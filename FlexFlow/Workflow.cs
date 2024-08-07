using Microsoft.Extensions.Logging;

namespace FlexFlow;

public sealed record Workflow<TInput, TOutput>
{
    private List<Func<object, Task<Result<object>>>> Steps { get; init; }
    private List<Func<Exception, Task<Result<object>>>> ExceptionHandlers { get; init; }
    private ILogger? Logger { get; init; }
    private TimeSpan? Timeout { get; init; }

    public Workflow(List<Func<object, Task<Result<object>>>> steps,
                    List<Func<Exception, Task<Result<object>>>> exceptionHandlers,
                    ILogger? logger = null,
                    TimeSpan? timeout = null)
    {
        Steps = steps;
        ExceptionHandlers = exceptionHandlers;
        Logger = logger;
        Timeout = timeout;
    }

    public async Task<Result<TOutput>> ExecuteAsync(TInput input)
    {
        ArgumentNullException.ThrowIfNull(input, nameof(input));
        object currentInput = input;

        try
        {
            foreach (var step in Steps)
            {
                Logger?.LogInformation("Executing step: {MethodName}", step.Method.Name);
                var stepTask = step(currentInput);

                if (Timeout.HasValue)
                {
                    var timeoutTask = Task.Delay(Timeout.Value);
                    var completedTask = await Task.WhenAny(stepTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        return Result<TOutput>.Failure("Step timed out");
                    }
                }

                var result = await stepTask;
                if (!result.IsSuccess)
                {
                    Logger?.LogError("Step failed: {Error}", result.Error);
                    return Result<TOutput>.Failure(result.Error);
                }
                currentInput = result.Value;
            }

            return Result<TOutput>.Success((TOutput)currentInput);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Workflow execution failed");
            var handlerResult = await TryHandleExceptionAsync(ex);
            return handlerResult ?? Result<TOutput>.Failure($"Unhandled exception: {ex.Message}");
        }
    }

    private async Task<Result<TOutput>?> TryHandleExceptionAsync(Exception ex)
    {
        foreach (var handler in ExceptionHandlers)
        {
            try
            {
                var result = await handler(ex);
                if (result.IsSuccess)
                {
                    return Result<TOutput>.Success((TOutput)result.Value);
                }
            }
            catch
            {
            }
        }
        return default;
    }
}