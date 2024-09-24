namespace HftCryptoTrading.Saga.StrategyEvaluator.Handlers;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Events;
using MediatR;
using Skender.Stock.Indicators;

public class NewPriceDetectedHandler(ExchangeProviderFactory exchangeProviderFactory, StrategyLoaderService strategyLoaderService) : INotificationHandler<SymbolAnalysePriceEvent>
{
    private readonly StrategyLoaderService _strategyLoaderService = strategyLoaderService;
    private readonly ExchangeProviderFactory _exchangeProviderFactory = exchangeProviderFactory;

    public async Task Handle(SymbolAnalysePriceEvent notification, CancellationToken cancellationToken)
    {
        //// Retrieve the compiled strategies
        //var strategies = _strategyLoaderService.GetCompiledStrategies();

        //// Use a ConcurrentBag to store the results with their priority
        //var results = new ConcurrentBag<(string Result, int Priority)>();

        //// Launch the execution of strategies in parallel
        //var tasks = strategies.Select(async strategyType =>
        //{
        //    // Create an instance of the strategy
        //    var strategyInstance = (IStrategy)Activator.CreateInstance(strategyType.Value, await GetQuotes(notification));

        //    // Execute the strategy and get the result
        //    var result = await Task.Run(() => strategyInstance.Execute());

        //    // Calculate the priority based on StrategyType and Priority
        //    var strategyPriority = (int)strategyInstance.StrategyType + strategyInstance.Priority;

        //    // Add the result and priority to the results bag
        //    results.Add((result, strategyPriority));
        //});

        //// Wait for all tasks to complete
        //await Task.WhenAll(tasks);

        //// Obtain the consensus from the results, considering priority
        //var consensus = GetConsensus(results.ToDictionary(r => r.Result, r => r.Priority));

        //// Additional logic based on the consensus
        //// For example, notify or execute an action based on the consensus
    }

    // Helper method to get the consensus considering priority
    private string GetConsensus(Dictionary<string, int> resultsWithPriority)
    {
        // Calculate the sum of priorities for each result
        var consensusResult = resultsWithPriority
            .OrderByDescending(r => r.Value)  // Sort by highest priority
            .FirstOrDefault();                // Select the result with the highest priority

        return consensusResult.Key ?? "No Consensus"; // Return the result with the highest weight
    }
}

