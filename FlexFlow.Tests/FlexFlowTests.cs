
using FlexFlow.Interfaces;
using FluentAssertions;

namespace FlexFlow.Tests;


[TestFixture]
public class WorkflowTests
{
    [Test]
    public async Task WorkFlow_Should_Return_Success_For_Valid_Message()
    {
        // Arrange
        var builder = new WorkflowBuilder<string, ValidatedMessage>();
        var workflow = new ValidMessageWorkflow();
        workflow.Build(builder);
        var builtWorkflow = await builder.BuildAsync();

        // Act
        var result = await builtWorkflow.ExecuteAsync("Valid message from Device-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Content.Should().Be("Valid message from Device-1");
        result.Value.DeviceId.Should().Be("Device-1");
    }

    [Test]
    public async Task WorkFlow_Should_Return_Failure_For_Invalid_Message()
    {
        // Arrange
        var builder = new WorkflowBuilder<string, ValidatedMessage>();
        var workflow = new InvalidMessageWorkflow();
        workflow.Build(builder);
        var builtWorkflow = await builder.BuildAsync();

        // Act
        var result = await builtWorkflow.ExecuteAsync("Invalid message from Device-2");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid message logged and rejected");
    }

    [Test]
    public async Task WorkFlow_Should_Execute_All_Steps_In_Branch()
    {
        // Arrange
        var builder = new WorkflowBuilder<string, ValidatedMessage>();
        var workflow = new TestWorkflow();
        workflow.Build(builder);
        var builtWorkflow = await builder.BuildAsync();

        // Act
        var validResult = await builtWorkflow.ExecuteAsync("Valid message from Device-3");
        var invalidResult = await builtWorkflow.ExecuteAsync("Invalid message from Device-4");

        // Assert
        validResult.IsSuccess.Should().BeTrue();
        validResult.Value.Should().NotBeNull();
        validResult.Value.IsValid.Should().BeTrue();

        invalidResult.IsSuccess.Should().BeFalse();
        invalidResult.Error.Should().Be("Invalid message logged and rejected");
    }
}

public class ValidMessageWorkflow : IWorkflow<string, ValidatedMessage>
{
    public void Build(IWorkflowBuilder<string, ValidatedMessage> builder)
    {
        builder
            .StartWith(input => Task.FromResult(Result<ParsedMessage>.Success(new ParsedMessage { Content = input, DeviceId = "Device-1" })))
            .Then(parsed => Task.FromResult(Result<ValidatedMessage>.Success(new ValidatedMessage { Content = parsed.Content, DeviceId = parsed.DeviceId, IsValid = true })));
    }
}

public class InvalidMessageWorkflow : IWorkflow<string, ValidatedMessage>
{
    public void Build(IWorkflowBuilder<string, ValidatedMessage> builder)
    {
        builder
            .StartWith(input => Task.FromResult(Result<ParsedMessage>.Success(new ParsedMessage { Content = input, DeviceId = "Device-2" })))
            .Then(parsed => Task.FromResult(Result<ValidatedMessage>.Failure("Invalid message logged and rejected")));
    }
}

public class TestWorkflow : IWorkflow<string, ValidatedMessage>
{
    public void Build(IWorkflowBuilder<string, ValidatedMessage> builder)
    {
        builder
            .StartWith(input => Task.FromResult(Result<ParsedMessage>.Success(new ParsedMessage { Content = input, DeviceId = input.Contains("Valid") ? "Device-3" : "Device-4" })))
            .Then(parsed => Task.FromResult(Result<ValidatedMessage>.Success(new ValidatedMessage { Content = parsed.Content, DeviceId = parsed.DeviceId, IsValid = parsed.Content.Contains("Valid") })))
            .Branch<ValidatedMessage>(
                message => Task.FromResult(message.IsValid),
                trueBuilder => Task.FromResult(trueBuilder
                    .Then(message =>
                    {
                        Console.Out.WriteLineAsync("Message is valid. Processing...");
                        return Task.FromResult(Result<ValidatedMessage>.Success(message));
                    })),
                falseBuilder => Task.FromResult(falseBuilder
                    .Then(message =>
                    {
                        Console.Out.WriteLineAsync("Message is invalid. Rejecting...");
                        return Task.FromResult(Result<ValidatedMessage>.Failure("Invalid message logged and rejected"));
                    }))
            );
    }
}