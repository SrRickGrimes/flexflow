using FlexFlow.Interfaces;

namespace FlexFlow.Tests;

public class ProductSaleWorkflow(
    IInventoryCheckStep inventoryCheckStep,
    IPriceCalculationStep priceCalculationStep,
    IDiscountApplicationStep discountApplicationStep,
    ISaleRegistrationStep saleRegistrationStep) : IWorkflow<Product, SaleProduct>
{
    public void Build(IWorkflowBuilder<Product, SaleProduct> builder)
    {
        builder
            .StartWith(inventoryCheckStep.ExecuteAsync)
            .Branch<SaleProduct>(
                saleProduct => Task.FromResult(saleProduct.IsAvailable),
                availableBuilder => Task.FromResult(availableBuilder
                    .StartWith(saleProduct => priceCalculationStep.ExecuteAsync(saleProduct))
                    .Then(saleProduct => discountApplicationStep.ExecuteAsync(saleProduct))
                    .Then(saleProduct => saleRegistrationStep.ExecuteAsync(saleProduct))),
                unavailableBuilder => Task.FromResult(unavailableBuilder
                    .StartWith(saleProduct => Task.FromResult(Result<SaleProduct>.Failure("Producto no disponible"))))
            );
    }
}
