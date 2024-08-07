using FlexFlow.Interfaces;

namespace FlexFlow.Tests;


public class ParsedMessage
{
    public required string Content { get; set; }
    public required string DeviceId { get; set; }
}

public class ValidatedMessage
{
    public required string Content { get; set; }
    public required string DeviceId { get; set; }
    public bool IsValid { get; set; }
}

public class ParseStep : IWorkflowStep<string, ParsedMessage>
{
    public Task<Result<ParsedMessage>> ExecuteAsync(string input)
    {
        var parsed = new ParsedMessage { Content = input, DeviceId = input.Contains("Valid") ? "Device-1" : "Device-2" };
        return Task.FromResult(Result<ParsedMessage>.Success(parsed));
    }
}

public class ValidateStep : IWorkflowStep<ParsedMessage, ValidatedMessage>
{
    public Task<Result<ValidatedMessage>> ExecuteAsync(ParsedMessage input)
    {
        var isValid = input.Content.Contains("Valid");
        var validated = new ValidatedMessage { Content = input.Content, DeviceId = input.DeviceId, IsValid = isValid };
        return Task.FromResult(Result<ValidatedMessage>.Success(validated));
    }
}

public class ProcessValidStep : IWorkflowStep<ValidatedMessage, ValidatedMessage>
{
    public Task<Result<ValidatedMessage>> ExecuteAsync(ValidatedMessage input)
    {
        Console.WriteLine($"Processing valid message: {input.Content}");
        return Task.FromResult(Result<ValidatedMessage>.Success(input));
    }
}

public class ProcessInvalidStep : IWorkflowStep<ValidatedMessage, ValidatedMessage>
{
    public Task<Result<ValidatedMessage>> ExecuteAsync(ValidatedMessage input)
    {
        Console.WriteLine($"Processing invalid message: {input.Content}");
        return Task.FromResult(Result<ValidatedMessage>.Failure("Invalid message processed"));
    }
}
