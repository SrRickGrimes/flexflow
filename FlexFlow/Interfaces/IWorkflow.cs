namespace FlexFlow.Interfaces;

public interface IWorkflow<TInput, TOutput>
{
    void Build(IWorkflowBuilder<TInput, TOutput> builder);
}
