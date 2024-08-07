using FlexFlow.Interfaces;

namespace FlexFlow.Tests;

public class Product
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required decimal BasePrice { get; set; }
}

public class SaleProduct
{
    public required Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsAvailable { get; set; }
}

public interface IInventoryCheckStep : IWorkflowStep<Product, SaleProduct> { }
public interface IPriceCalculationStep : IWorkflowStep<SaleProduct, SaleProduct> { }
public interface IDiscountApplicationStep : IWorkflowStep<SaleProduct, SaleProduct> { }
public interface ISaleRegistrationStep : IWorkflowStep<SaleProduct, SaleProduct> { }

public class InventoryCheckStep : IInventoryCheckStep
{
    public Task<Result<SaleProduct>> ExecuteAsync(Product input)
    {
        var isAvailable = input.Id != "OUT_OF_STOCK";
        var saleProduct = new SaleProduct
        {
            Product = input,
            Quantity = isAvailable ? 1 : 0,
            IsAvailable = isAvailable
        };
        return Task.FromResult(Result<SaleProduct>.Success(saleProduct));
    }
}

public class PriceCalculationStep : IPriceCalculationStep
{
    public Task<Result<SaleProduct>> ExecuteAsync(SaleProduct input)
    {
        input.FinalPrice = input.Product.BasePrice * input.Quantity;
        return Task.FromResult(Result<SaleProduct>.Success(input));
    }
}

public class DiscountApplicationStep : IDiscountApplicationStep
{
    public Task<Result<SaleProduct>> ExecuteAsync(SaleProduct input)
    {
        if (input.FinalPrice > 100)
        {
            input.FinalPrice *= 0.9m;
        }
        return Task.FromResult(Result<SaleProduct>.Success(input));
    }
}

public class SaleRegistrationStep : ISaleRegistrationStep
{
    public Task<Result<SaleProduct>> ExecuteAsync(SaleProduct input)
    {
        Console.WriteLine($"Sell registered: {input.Product.Name}, Quantity: {input.Quantity}, Final Price: {input.FinalPrice}");
        return Task.FromResult(Result<SaleProduct>.Success(input));
    }
}
