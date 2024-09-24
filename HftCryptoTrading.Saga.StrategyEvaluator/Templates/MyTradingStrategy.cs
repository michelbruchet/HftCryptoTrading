using System;
using System.Collections.Generic;

public class MyTradingStrategy : HftCryptoTrading.Shared.Strategies.IStrategy
{
    private List<Quote> quotesList;

    // Constructeur qui prend la liste des prix
    public MyTradingStrategy(List<Quote> quotes)
    {
        quotesList = quotes;
    }

    public void Execute()
    {
        if (quotesList.Count == 0)
        {
            Console.WriteLine("Aucune donnée disponible.");
            return;
        }

        // Obtenir le dernier prix
        var lastQuote = quotesList[^1]; // C# 8.0 ou plus pour obtenir le dernier élément
        var recentQuotes = quotesList.GetRange(Math.Max(0, quotesList.Count - 14), Math.Min(14, quotesList.Count)); // Derniers 14 prix pour l'indicateur

        // Calculer l'indicateur Stochastic RSI
        List<StochRsiResult> resultsList = GetStochRsi(recentQuotes, 14, 14, 3, 1);

        // Assurez-vous que nous avons des résultats suffisants pour prendre des décisions
        if (resultsList.Count == 0)
        {
            Console.WriteLine("Pas assez de données pour calculer Stoch RSI.");
            return;
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
            Console.WriteLine($"LONG signal pour {lastQuote.Date:yyyy-MM-dd} à {tradingPrice:c2}.");
        }

        // Vérifier les conditions SHORT
        if (previousResult.StochRsi >= 80 && previousResult.StochRsi > previousResult.Signal && tradingQuantity != -1)
        {
            tradingQuantity = -1; // Ouvrir position SHORT
            Console.WriteLine($"SHORT signal pour {lastQuote.Date:yyyy-MM-dd} à {tradingPrice:c2}.");
        }
    }

    private List<StochRsiResult> GetStochRsi(List<Quote> quotes, int rsiPeriod, int stochPeriod, int smoothK, int smoothD)
    {
        // Implémentez la logique pour calculer le Stochastic RSI
        // Retourner une liste de résultats StochRsiResult
        return new List<StochRsiResult>();
    }
}

public class StochRsiResult
{
    public decimal StochRsi { get; set; }
    public decimal Signal { get; set; }
}
