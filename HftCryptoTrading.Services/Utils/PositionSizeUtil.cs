using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Services.Utils;

public interface IPositionSizeUtil
{
    Task<decimal> FixSellMarketQuantity(decimal quantity, SymbolData symbol, CancellationToken cancellationToken);
}

public class PositionSizeUtil : IPositionSizeUtil
{
    public async Task<decimal> FixSellMarketQuantity(decimal quantity, SymbolData symbol, CancellationToken cancellationToken)
    {
        // Step 1: Ensure the quantity conforms to the minimum and maximum lot size

        // Check the minimum quantity
        if (symbol.MarketLotSizeFilter.MinQuantity > 0 && quantity < symbol.MarketLotSizeFilter.MinQuantity)
        {
            // Adjust to the minimum quantity
            quantity = symbol.MarketLotSizeFilter.MinQuantity;
        }

        // Check the maximum quantity
        if (symbol.MarketLotSizeFilter.MaxQuantity > 0 && quantity > symbol.MarketLotSizeFilter.MaxQuantity)
        {
            // Adjust to the maximum quantity
            quantity = symbol.MarketLotSizeFilter.MaxQuantity;
        }

        // Step 2: Adjust the quantity to respect the Step Size
        if (symbol.MarketLotSizeFilter.StepSize > 0)
        {
            // Adjust the quantity to be a multiple of the Step Size
            quantity = Math.Floor(quantity / symbol.MarketLotSizeFilter.StepSize) * symbol.MarketLotSizeFilter.StepSize;
        }

        // Step 3: Validate against the minimum notional rule (Min Notional)
        if (symbol.MinNotionalFilter.MinNotional > 0 && symbol.PriceFilter.MinPrice > 0)
        {
            decimal notionalValue = quantity * symbol.PriceFilter.MinPrice;

            // If the notional value is less than the minimum required, adjust the quantity
            if (notionalValue < symbol.MinNotionalFilter.MinNotional)
            {
                // Calculate the minimum required quantity to meet the notional value
                decimal minRequiredQuantity = symbol.MinNotionalFilter.MinNotional / symbol.PriceFilter.MinPrice;

                // Adjust the quantity to this minimum while respecting the Step Size
                quantity = Math.Ceiling(minRequiredQuantity / symbol.MarketLotSizeFilter.StepSize) * symbol.MarketLotSizeFilter.StepSize;

                // Ensure this quantity does not exceed the maximum quantity
                if (symbol.MarketLotSizeFilter.MaxQuantity > 0 && quantity > symbol.MarketLotSizeFilter.MaxQuantity)
                {
                    quantity = symbol.MarketLotSizeFilter.MaxQuantity;
                }
            }
        }

        // Step 4: Ensure the quantity does not exceed the maximum position (Max Position)
        var maxPosition = symbol.PriceFilter.MaxPrice * quantity;

        if (maxPosition > 0 && maxPosition > symbol.MaxPositionFilter.MaxPosition)
        {
            // Limit the quantity to the maximum allowed position
            var maxAllowedQuantity = symbol.MaxPositionFilter.MaxPosition / symbol.PriceFilter.MaxPrice;
            quantity = Math.Ceiling(maxAllowedQuantity / symbol.MarketLotSizeFilter.StepSize) * symbol.MarketLotSizeFilter.StepSize;
        }

        return quantity;
    }
}
