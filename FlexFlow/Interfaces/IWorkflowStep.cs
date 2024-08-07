namespace FlexFlow.Interfaces;

public interface IWorkflowStep<TInput, TOutput>
{
    Task<Result<TOutput>> ExecuteAsync(TInput input);
}