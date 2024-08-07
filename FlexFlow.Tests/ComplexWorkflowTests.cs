using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace FlexFlow.Tests;

[TestFixture]
public class ComplexWorkflowTests
{
    private Mock<ILogger<ComplexWorkflow>> _loggerMock;
    private ComplexWorkflow _workflow;
    private WorkflowBuilder<int, Order> _builder;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ComplexWorkflow>>();
        _workflow = new ComplexWorkflow(_loggerMock.Object);
        _builder = new WorkflowBuilder<int, Order>();
    }

    [Test]
    public async Task ComplexWorkflow_ShouldProcessOrderSuccessfully()
    {
        // Arrange
        _workflow.Build(_builder);
        var builtWorkflow = await _builder.BuildAsync();

        // Act
        var result = await builtWorkflow.ExecuteAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(1);
        result.Value.IsProcessed.Should().BeTrue();
        result.Value.IsShipped.Should().BeTrue();

        if (result.Value.TotalAmount > 1000)
        {
            result.Value.TotalAmount.Should().BeLessThan(2000 * 0.9m);
        }
        else
        {
            result.Value.TotalAmount.Should().BeLessThanOrEqualTo(1000);
        }
    }
}