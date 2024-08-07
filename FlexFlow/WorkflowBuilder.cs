using FlexFlow.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlexFlow;

public sealed class WorkflowBuilder<TInput, TOutput> : IWorkflowBuilder<TInput, TOutput>
{
    private readonly List<Func<object, Task<Result<object>>>> _steps = [];
    private readonly List<Func<Exception, Task<Result<object>>>> _exceptionHandlers = [];
    private ILogger? _logger;
    private TimeSpan? _timeout;

    public IWorkflowBuilder<TInput, TNextOutput> StartWith<TNextOutput>(Func<TInput, Task<Result<TNextOutput>>> step)
    {
        _steps.Add(async input => await ConvertResultAsync(step((TInput)input)));
        return new WorkflowBuilder<TInput, TNextOutput>(_steps, _exceptionHandlers, _logger, _timeout);
    }

    public IWorkflowBuilder<TInput, TNextOutput> Then<TNextOutput>(Func<TOutput, Task<Result<TNextOutput>>> step)
    {
        _steps.Add(async input => await ConvertResultAsync(step((TOutput)input)));
        return new WorkflowBuilder<TInput, TNextOutput>(_steps, _exceptionHandlers, _logger, _timeout);
    }

    public IWorkflowBuilder<TInput, TOutput> Branch<TCurrentOutput>(
        Func<TCurrentOutput, Task<bool>> condition,
        Func<IWorkflowBuilder<TCurrentOutput, TOutput>, Task<IWorkflowBuilder<TCurrentOutput, TOutput>>> trueBranch,
        Func<IWorkflowBuilder<TCurrentOutput, TOutput>, Task<IWorkflowBuilder<TCurrentOutput, TOutput>>> falseBranch)
    {
        _steps.Add(async input =>
        {
            var currentInput = (TCurrentOutput)input;
            if (await condition(currentInput))
            {
                var trueBuilder = new WorkflowBuilder<TCurrentOutput, TOutput>();
                var builtTrueBranch = await trueBranch(trueBuilder);
                var trueWorkflow = await builtTrueBranch.BuildAsync();
                return ConvertResult(await trueWorkflow.ExecuteAsync(currentInput));
            }
            else
            {
                var falseBuilder = new WorkflowBuilder<TCurrentOutput, TOutput>();
                var builtFalseBranch = await falseBranch(falseBuilder);
                var falseWorkflow = await builtFalseBranch.BuildAsync();
                return ConvertResult(await falseWorkflow.ExecuteAsync(currentInput));
            }
        });
        return this;
    }

    public IWorkflowBuilder<TInput, TOutput> Catch<TException>(Func<TException, Task<Result<TOutput>>> handler) where TException : Exception
    {
        _exceptionHandlers.Add(async ex =>
        {
            if (ex is TException typedException)
            {
                return await ConvertResultAsync(handler(typedException));
            }
            throw ex;
        });
        return this;
    }

    public IWorkflowBuilder<TInput, TOutput> Retry(int maxAttempts, TimeSpan delay)
    {
        var lastStep = _steps[^1];
        _steps[^1] = async input =>
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    var result = await lastStep(input);
                    if (result.IsSuccess)
                    {
                        return result;
                    }
                }
                catch
                {
                    if (attempt == maxAttempts - 1)
                    {
                        throw;
                    }
                }
                await Task.Delay(delay);
            }
            throw new Exception($"Step failed after {maxAttempts} attempts");
        };
        return this;
    }

    public IWorkflowBuilder<TInput, TOutput> If(
        Func<TOutput, Task<bool>> condition,
        Func<IWorkflowBuilder<TOutput, TOutput>, Task<IWorkflowBuilder<TOutput, TOutput>>> trueBranch) => Branch<TOutput>(
            condition,
            trueBranch,
            falseBuilder => Task.FromResult(falseBuilder)
        );

    public IWorkflowBuilder<TInput, TOutput> While(
        Func<TOutput, Task<bool>> condition,
        Func<IWorkflowBuilder<TOutput, TOutput>, Task<IWorkflowBuilder<TOutput, TOutput>>> body)
    {
        _steps.Add(async input =>
        {
            var currentInput = (TOutput)input;
            while (await condition(currentInput))
            {
                var bodyBuilder = new WorkflowBuilder<TOutput, TOutput>();
                var builtBody = await body(bodyBuilder);
                var bodyWorkflow = await builtBody.BuildAsync();
                var result = await bodyWorkflow.ExecuteAsync(currentInput);
                if (!result.IsSuccess)
                {
                    return Result<object>.Failure(result.Error);
                }
                currentInput = result.Value;
            }
            if(currentInput is not null)
            {
                return Result<object>.Success(currentInput);
            }
            return Result<object>.Failure("Errror executing the workflow");
        });
        return this;
    }

    public IWorkflowBuilder<TInput, TOutput> WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    public IWorkflowBuilder<TInput, TOutput> WithLogging(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    public IWorkflowBuilder<TInput, TNextOutput> Map<TNextOutput>(Func<TOutput, TNextOutput> mapper)
    {
        _steps.Add(input => Task.FromResult(Result<object>.Success(mapper((TOutput)input)!)));
        return new WorkflowBuilder<TInput, TNextOutput>(_steps, _exceptionHandlers, _logger, _timeout);
    }

    public IWorkflowBuilder<TInput, TOutput> SubWorkflow<TSubInput, TSubOutput>(
        IWorkflow<TSubInput, TSubOutput> subWorkflow,
        Func<TOutput, TSubInput> inputMapper,
        Func<TSubOutput, TOutput> outputMapper)
    {
        _steps.Add(async input =>
        {
            var subInput = inputMapper((TOutput)input);
            var subBuilder = new WorkflowBuilder<TSubInput, TSubOutput>();
            subWorkflow.Build(subBuilder);
            var builtSubWorkflow = await subBuilder.BuildAsync();
            var subResult = await builtSubWorkflow.ExecuteAsync(subInput);
            if (subResult.IsSuccess && subResult.Value is not null)
            {
                return Result<object>.Success(outputMapper(subResult.Value)!);
            }
            return Result<object>.Failure(subResult.Error);
        });
        return this;
    }

    private static Result<object> ConvertResult<T>(Result<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Result<object>.Success(result.Value);
        }
        return Result<object>.Failure(result.Error);
    }
    public Task<Workflow<TInput, TOutput>> BuildAsync()
    {
        return Task.FromResult(new Workflow<TInput, TOutput>(_steps, _exceptionHandlers, _logger, _timeout));
    }

    private static async Task<Result<object>> ConvertResultAsync<T>(Task<Result<T>> resultTask)
    {
        var result = await resultTask;
        return result.IsSuccess && result.Value is not null
            ? Result<object>.Success(result.Value)
            : Result<object>.Failure(result.Error);
    }

    private WorkflowBuilder(
        List<Func<object, Task<Result<object>>>> steps,
        List<Func<Exception, Task<Result<object>>>> exceptionHandlers,
        ILogger? logger,
        TimeSpan? timeout)
    {
        _steps = steps;
        _exceptionHandlers = exceptionHandlers;
        _logger = logger;
        _timeout = timeout;
    }
    public WorkflowBuilder() { }
}
