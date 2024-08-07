using FlexFlow.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlexFlow.Tests;

public class Order
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsShipped { get; set; }
    public int RetryCount { get; set; }
}

public class ProcessingException(string message) : Exception(message)
{
}

public class ComplexWorkflow(ILogger<ComplexWorkflow> logger) : IWorkflow<int, Order>
{
    public void Build(IWorkflowBuilder<int, Order> builder)
    {
        builder
            .StartWith(CreateOrder)
            .Then(ValidateOrder)
            .Branch<Order>(
                order => Task.FromResult(order.TotalAmount > 1000),
                highValueBranch => Task.FromResult(highValueBranch
                    .Then(ApplyDiscount)
                    .Then(ProcessHighValueOrder)),
                lowValueBranch => Task.FromResult(lowValueBranch
                    .Then(ProcessLowValueOrder))
            )
            .Then(ProcessPayment)
            .Retry(3, TimeSpan.FromSeconds(1))
            .Catch<ProcessingException>(HandleProcessingException)
            .If(
                order => Task.FromResult(!order.IsProcessed),
                retryBranch => Task.FromResult(retryBranch.Then(RetryProcessing))
            )
            .While(
                order => Task.FromResult(!order.IsShipped),
                shippingBranch => Task.FromResult(shippingBranch.Then(AttemptShipping))
            )
            .Map(FinalizeOrder)
            .WithLogging(logger);
    }

    private Task<Result<Order>> CreateOrder(int orderId)
    {
        var order = new Order { Id = orderId, TotalAmount = new Random().Next(500, 2000) };
        return Task.FromResult(Result<Order>.Success(order));
    }

    private Task<Result<Order>> ValidateOrder(Order order)
    {
        if (order.TotalAmount <= 0)
        {
            return Task.FromResult(Result<Order>.Failure("Invalid order amount"));
        }
        return Task.FromResult(Result<Order>.Success(order));
    }

    private Task<Result<Order>> ApplyDiscount(Order order)
    {
        order.TotalAmount *= 0.9m;
        return Task.FromResult(Result<Order>.Success(order));
    }

    private async Task<Result<Order>> ProcessHighValueOrder(Order order)
    {
        // Simulate complex processing
        await Task.Delay(100);
        order.IsProcessed = true;
        return Result<Order>.Success(order);
    }

    private Task<Result<Order>> ProcessLowValueOrder(Order order)
    {
        // Simulate simple processing
        order.IsProcessed = true;
        return Task.FromResult(Result<Order>.Success(order));
    }

    private Task<Result<Order>> ProcessPayment(Order order)
    {
        if (new Random().Next(0, 10) < 3) // 80% chance of failure
        {
            throw new ProcessingException("Payment processing failed");
        }
        return Task.FromResult(Result<Order>.Success(order));
    }

    private Task<Result<Order>> HandleProcessingException(ProcessingException ex)
    {
        return Task.FromResult(Result<Order>.Failure($"Handled processing exception: {ex.Message}"));
    }

    private Task<Result<Order>> RetryProcessing(Order order)
    {
        order.RetryCount++;
        order.IsProcessed = true;
        return Task.FromResult(Result<Order>.Success(order));
    }

    private Task<Result<Order>> AttemptShipping(Order order)
    {
        if (new Random().Next(0, 10) < 8) // 80% chance of successful shipping
        {
            order.IsShipped = true;
        }
        return Task.FromResult(Result<Order>.Success(order));
    }

    private Order FinalizeOrder(Order order)
    {
        order.IsProcessed = true;
        order.IsShipped = true;
        return order;
    }
}
