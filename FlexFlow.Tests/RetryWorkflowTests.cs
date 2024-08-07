using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using FlexFlow.Tests;
using FlexFlow;

namespace FlexFlow.Tests;

[TestFixture]
public class RetryWorkflowTests
{
    private Mock<ILogger<RetryWorkflow>> _loggerMock;
    private RetryWorkflow _workflow;
    private WorkflowBuilder<string, ConnectionResult> _builder;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<RetryWorkflow>>();
        _workflow = new RetryWorkflow(_loggerMock.Object);
        _builder = new WorkflowBuilder<string, ConnectionResult>();
    }

    [Test]
    public async Task RetryWorkflow_ShouldEventuallyConnect()
    {
        // Arrange
        _workflow.Build(_builder);
        var builtWorkflow = await _builder.BuildAsync();

        // Act
        var result = await builtWorkflow.ExecuteAsync("test-server.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.IsConnected.Should().BeTrue();
        result.Value.AttemptCount.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(5);
        result.Value.Message.Should().Contain("Successfully connected");
    }

    [Test]
    public async Task RetryWorkflow_ShouldHandleAllFailedAttempts()
    {
        // Arrange
        _workflow = new RetryWorkflow(_loggerMock.Object, true);
        _workflow.Build(_builder);
        var builtWorkflow = await _builder.BuildAsync();

        // Act
        var result = await builtWorkflow.ExecuteAsync("unreachable-server.com");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}