using Microsoft.Extensions.Logging;

namespace FlexFlow.Interfaces;

public interface IWorkflowBuilder<TInput, TOutput>
{
    IWorkflowBuilder<TInput, TNextOutput> StartWith<TNextOutput>(Func<TInput, Task<Result<TNextOutput>>> step);
    IWorkflowBuilder<TInput, TNextOutput> Then<TNextOutput>(Func<TOutput, Task<Result<TNextOutput>>> step);
    IWorkflowBuilder<TInput, TOutput> Branch<TCurrentOutput>(
        Func<TCurrentOutput, Task<bool>> condition,
        Func<IWorkflowBuilder<TCurrentOutput, TOutput>, Task<IWorkflowBuilder<TCurrentOutput, TOutput>>> trueBranch,
        Func<IWorkflowBuilder<TCurrentOutput, TOutput>, Task<IWorkflowBuilder<TCurrentOutput, TOutput>>> falseBranch);

    IWorkflowBuilder<TInput, TOutput> Catch<TException>(Func<TException, Task<Result<TOutput>>> handler) where TException : Exception;
    IWorkflowBuilder<TInput, TOutput> Retry(int maxAttempts, TimeSpan delay);

    IWorkflowBuilder<TInput, TOutput> If(
        Func<TOutput, Task<bool>> condition,
        Func<IWorkflowBuilder<TOutput, TOutput>, Task<IWorkflowBuilder<TOutput, TOutput>>> trueBranch);

    IWorkflowBuilder<TInput, TOutput> While(
        Func<TOutput, Task<bool>> condition,
        Func<IWorkflowBuilder<TOutput, TOutput>, Task<IWorkflowBuilder<TOutput, TOutput>>> body);

    IWorkflowBuilder<TInput, TOutput> WithLogging(ILogger logger);

    IWorkflowBuilder<TInput, TNextOutput> Map<TNextOutput>(Func<TOutput, TNextOutput> mapper);

    Task<Workflow<TInput, TOutput>> BuildAsync();
}