using FlexFlow.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlexFlow.Tests;

public class ConnectionResult
{
    public bool IsConnected { get; set; }
    public int AttemptCount { get; set; }
    public required string Message { get; set; }
}

public class RetryWorkflow(ILogger<RetryWorkflow> logger, bool forceAllFailures = false) : IWorkflow<string, ConnectionResult>
{
    private readonly Random _random = new();
    public void Build(IWorkflowBuilder<string, ConnectionResult> builder)
    {
        builder
            .StartWith(InitializeConnection)
            .Then(AttemptConnection)
            .Retry(5, TimeSpan.FromSeconds(1))
            .Then(FinalizeConnection)
            .Catch<Exception>(HandleConnectionException)
            .WithLogging(logger);
    }

    private Task<Result<ConnectionResult>> InitializeConnection(string serverAddress)
    {
        logger.LogInformation("Initializing connection to {}", serverAddress);
        return Task.FromResult(Result<ConnectionResult>.Success(new ConnectionResult
        {
            IsConnected = false,
            AttemptCount = 0,
            Message = $"Initializing connection to {serverAddress}"
        }));
    }

    private Task<Result<ConnectionResult>> AttemptConnection(ConnectionResult result)
    {
        result.AttemptCount++;
        logger.LogInformation("Attempt {} to connect", result.AttemptCount);

        if (forceAllFailures || _random.NextDouble() > 0.3)
        {
            logger.LogWarning("Connection failed on attempt {}", result.AttemptCount);
            return Task.FromResult(Result<ConnectionResult>.Failure($"Connection failed on attempt {result.AttemptCount}"));
        }

        result.IsConnected = true;
        result.Message = $"Successfully connected on attempt {result.AttemptCount}";
        return Task.FromResult(Result<ConnectionResult>.Success(result));
    }

    private Task<Result<ConnectionResult>> FinalizeConnection(ConnectionResult result)
    {
        if (result.IsConnected)
        {
            logger.LogInformation("Connection finalized successfully");
            result.Message += " - Connection finalized";
        }
        else
        {
            logger.LogError("Failed to establish connection after all attempts");
            result.Message = "Failed to establish connection after all attempts";
        }
        return Task.FromResult(Result<ConnectionResult>.Success(result));
    }

    private Task<Result<ConnectionResult>> HandleConnectionException(Exception ex)
    {
        logger.LogError(ex, "An unexpected error occurred during connection");
        return Task.FromResult(Result<ConnectionResult>.Failure($"An unexpected error occurred: {ex.Message}"));
    }
}
