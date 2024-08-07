using FluentAssertions;


namespace FlexFlow.Tests;


[TestFixture]
public class WorkflowBuilderTests
{
    [Test]
    public async Task Catch_HandlesSpecificException()
    {
        var builder = new WorkflowBuilder<string, int>();
        builder
            .StartWith<int>(input => throw new ArgumentException("Test exception"))
            .Catch<ArgumentException>(ex => Task.FromResult(Result<int>.Success(42)));

        var workflow = await builder.BuildAsync();
        var result = await workflow.ExecuteAsync("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Test]
    public async Task Retry_RetriesFailedStep()
    {
        var attempts = 0;
        var builder = new WorkflowBuilder<string, int>();
        builder
            .StartWith<int>(input =>
            {
                attempts++;
                if (attempts < 3)
                    return Task.FromResult(Result<int>.Failure("Failing"));
                return Task.FromResult(Result<int>.Success(42));
            })
            .Retry(3, TimeSpan.FromMilliseconds(10));

        var workflow = await builder.BuildAsync();
        var result = await workflow.ExecuteAsync("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        attempts.Should().Be(3);
    }

    [Test]
    public async Task If_ExecutesTrueBranchWhenConditionIsTrue()
    {
        var builder = new WorkflowBuilder<string, int>();
        builder
            .StartWith(input => Task.FromResult(Result<int>.Success(5)))
            .If(
                value => Task.FromResult(value > 0),
                trueBuilder => Task.FromResult(trueBuilder.Then(value => Task.FromResult(Result<int>.Success(value * 2))))
            );

        var workflow = await builder.BuildAsync();
        var result = await workflow.ExecuteAsync("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Test]
    public async Task While_ExecutesBodyWhileConditionIsTrue()
    {
        var builder = new WorkflowBuilder<string, int>();
        builder
            .StartWith(input => Task.FromResult(Result<int>.Success(0)))
            .While(
                value => Task.FromResult(value < 3),
                bodyBuilder => Task.FromResult(bodyBuilder.Then(value => Task.FromResult(Result<int>.Success(value + 1))))
            );

        var workflow = await builder.BuildAsync();
        var result = await workflow.ExecuteAsync("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Test]
    public async Task Map_TransformsOutput()
    {
        var builder = new WorkflowBuilder<string, int>();
        builder
            .StartWith(input => Task.FromResult(Result<int>.Success(21)))
            .Map(value => value * 2);

        var workflow = await builder.BuildAsync();
        var result = await workflow.ExecuteAsync("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }
}