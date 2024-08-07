using FlexFlow.Interfaces;

namespace FlexFlow.Tests;

public class BranchingWorkflow : IWorkflow<string, ValidatedMessage>
{
    public void Build(IWorkflowBuilder<string, ValidatedMessage> builder)
    {
        builder
            .StartWith(input => new ParseStep().ExecuteAsync(input))
            .Then(parsed => new ValidateStep().ExecuteAsync(parsed))
            .Branch<ValidatedMessage>(
                message => Task.FromResult(message.IsValid),
                trueBuilder => Task.FromResult(trueBuilder
                    .StartWith(message => new ProcessValidStep().ExecuteAsync(message))),
                falseBuilder => Task.FromResult(falseBuilder
                    .StartWith(message => new ProcessInvalidStep().ExecuteAsync(message)))
            );
    }
}
