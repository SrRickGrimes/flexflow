using FluentAssertions;


namespace FlexFlow.Tests;

[TestFixture]
public class BranchingWorkflowTests
{
    private WorkflowBuilder<string, ValidatedMessage> _builder;
    private Workflow<string, ValidatedMessage> _workflow;

    [SetUp]
    public async Task Setup()
    {
        _builder = new WorkflowBuilder<string, ValidatedMessage>();
        var workflowDefinition = new BranchingWorkflow();
        workflowDefinition.Build(_builder);
        _workflow = await _builder.BuildAsync();
    }

    [Test]
    public async Task Workflow_Should_Process_Valid_Message_Successfully()
    {
        // Arrange
        var input = "Valid message from Device-1";

        // Act
        var result = await _workflow.ExecuteAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Content.Should().Be(input);
        result.Value.DeviceId.Should().Be("Device-1");
    }

    [Test]
    public async Task Workflow_Should_Process_Invalid_Message_As_Failure()
    {
        // Arrange
        var input = "Invalid message from Device-2";

        // Act
        var result = await _workflow.ExecuteAsync(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid message processed");
    }

    [Test]
    public async Task Workflow_Should_Handle_Edge_Case_Messages()
    {
        // Arrange
        var edgeCaseInput = "Edge case Valid message from Device-3";

        // Act
        var result = await _workflow.ExecuteAsync(edgeCaseInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Content.Should().Be(edgeCaseInput);
        result.Value.DeviceId.Should().Be("Device-1"); // Because it contains "Valid"
    }

    [Test]
    public async Task Workflow_Should_Handle_Empty_Input()
    {
        // Arrange
        var emptyInput = "";

        // Act
        var result = await _workflow.ExecuteAsync(emptyInput);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid message processed");
    }
}