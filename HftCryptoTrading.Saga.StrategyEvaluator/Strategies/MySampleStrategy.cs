using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;

public class MySampleStrategy : IStrategy
{
    private List<Quote> quotesList;

    // Constructeur qui prend la liste des prix
    public MySampleStrategy(List<Quote> quotes)
    {
        quotesList = quotes;
    }

    public string StrategyName => "MySampleStrategy";

    public string Description => "It will buy-to-open (BTO) one share when the Stoch RSI (%K) is below 20 and crosses over the Signal \r\n\t(%D). The reverse Sell-to-Close (STC) and Sell-To-Open (STO) occurs when the Stoch RSI is above 80 and crosses below the Signal.";

    public StrategyType StrategyType => StrategyType.General;

    public int Priority => 100;

    public string Error { get; private set; }

    public ActionStrategy Execute()
    {
        if (quotesList.Count == 0)
        {
            Error = ("Aucune donnée disponible.");
            return ActionStrategy.Error;
        }

        // Obtenir le dernier prix
        var lastQuote = quotesList[^1]; // C# 8.0 ou plus pour obtenir le dernier élément
        var recentQuotes = quotesList.GetRange(Math.Max(0, quotesList.Count - 14), Math.Min(14, quotesList.Count)); // Derniers 14 prix pour l'indicateur

        // Calculer l'indicateur Stochastic RSI
        List<StochRsiResult> resultsList = quotesList.GetStochRsi(14, 14, 3, 1).ToList();

        // Assurez-vous que nous avons des résultats suffisants pour prendre des décisions
        if (resultsList.Count == 0)
        {
            Error = ("Pas assez de données pour calculer Stoch RSI.");
            return ActionStrategy.Error;
        }

        var currentResult = resultsList[^1]; // Résultat le plus récent
        var previousResult = resultsList[^2]; // Résultat précédent (s'il existe)

        // Initialisation des variables
        decimal tradingPrice = lastQuote.Close;
        decimal tradingQuantity = 0;
        decimal unrealizedGain = tradingQuantity * (tradingPrice - tradingPrice); // Initialement zéro

        // Vérifier les conditions LONG
        if (previousResult.StochRsi <= 20 && previousResult.StochRsi < previousResult.Signal && tradingQuantity != 1)
        {
            tradingQuantity = 1; // Ouvrir position LONG
            return ActionStrategy.Long;
        }

        // Vérifier les conditions SHORT
        if (previousResult.StochRsi >= 80 && previousResult.StochRsi > previousResult.Signal && tradingQuantity != -1)
        {
            tradingQuantity = -1; // Ouvrir position SHORT
            return ActionStrategy.Short;
        }

        return ActionStrategy.Hold;
    }
}