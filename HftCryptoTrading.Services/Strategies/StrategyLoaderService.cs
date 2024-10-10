using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Strategies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyInjection;
using Skender.Stock.Indicators;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace StrategyExecution;

public class StrategyLoaderService(IMetricService metricService)
{
    private string _strategiesPath;

    private ConcurrentDictionary<string, IStrategy> _strategies = new();
    private IMetricService _metricService = metricService;

    // Load strategies at the start
    public void LoadStrategies(string strategiesPath)
    {
        using var tracker = _metricService.StartTracking("LoadStrategies");
        _strategiesPath = strategiesPath;

        if (!Directory.Exists(_strategiesPath))
        {
            _metricService.TrackFailure("LoadStrategies");
            throw new DirectoryNotFoundException($"The specified strategies directory does not exist: {_strategiesPath}");
        }

        var strategyFiles = Directory.GetFiles(_strategiesPath, "*.cs");

        foreach (var file in strategyFiles)
        {
            CompileStrategy(file);
        }

        _metricService.TrackSuccess("LoadStrategies");
    }

    // Compile and add strategy from file
    private void CompileStrategy(string filePath)
    {
        using var tracker = _metricService.StartTracking("CompileStrategy");

        try
        {
            var code = File.ReadAllText(filePath);
            var wrappedCode = AddUsingsAndNamespace(code, "StrategyBusiness");

            var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);
            var strategyName = Path.GetFileNameWithoutExtension(filePath);
            var references = GetReferences();

            var compilation = CSharpCompilation.Create(
                strategyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        _metricService.TrackFailure("CompileStrategy");
                        throw new InvalidOperationException($"Error compiling strategy {strategyName}: {diagnostic.GetMessage()}");
                    }
                }
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IStrategy).IsAssignableFrom(type))
                    {
                        var strategyInstance = (IStrategy)Activator.CreateInstance(type);
                        _strategies[strategyName] = strategyInstance;
                    }
                }
            }

            _metricService.TrackSuccess("CompileStrategy");
        }
        catch (Exception ex)
        {
            _metricService.TrackFailure("CompileStrategy", ex);
            throw;
        }
    }

    // Ajouter les usings et le namespace au code
    private string AddUsingsAndNamespace(string code, string namespaceName)
    {
        var usingDirectives = @"
        //added automatically by the generator
        using System;
        using Skender.Stock.Indicators;
        using System.Collections.Generic;
        using System.Linq;
        using HftCryptoTrading.Shared.Strategies;
        //end added automatically by the generator
        ";

        var @namespace = new StringBuilder("HftCryptoTrading.CustomStragies");
        @namespace.Append(namespaceName);

        var fullcode = new StringBuilder();

        fullcode.AppendLine(usingDirectives);
        fullcode.AppendLine();
        fullcode.AppendLine($"namespace {@namespace.ToString()};");
        fullcode.AppendLine();

        fullcode.AppendLine(code);

        return fullcode.ToString();
    }

    // Simuler les références requises pour la compilation
    private IEnumerable<MetadataReference> GetReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var references = new List<MetadataReference>();

        foreach (var assembly in assemblies)
        {
            if (!string.IsNullOrEmpty(assembly.Location) && !assembly.IsDynamic)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var skenderAssembly = typeof(Skender.Stock.Indicators.Indicator).Assembly;
        references.Add(MetadataReference.CreateFromFile(skenderAssembly.Location));

        return references;
    }

    // Add a new strategy
    public void AddStrategy(string filePath)
    {
        using var tracker = _metricService.StartTracking("AddStrategy");

        try
        {
            if (File.Exists(filePath))
            {
                CompileStrategy(filePath);
            }
            else
            {
                _metricService.TrackFailure("AddStrategy");
                throw new FileNotFoundException($"Strategy file not found: {filePath}");
            }

            _metricService.TrackSuccess("AddStrategy");
        }
        catch (Exception ex)
        {
            _metricService.TrackFailure("AddStrategy", ex);
            throw;
        }
    }

    // Update an existing strategy
    public void UpdateStrategy(string filePath)
    {
        using var tracker = _metricService.StartTracking("UpdateStrategy");

        try
        {
            var strategyName = Path.GetFileNameWithoutExtension(filePath);
            RemoveStrategy(strategyName);
            CompileStrategy(filePath);
            _metricService.TrackSuccess("UpdateStrategy");
        }
        catch (Exception ex)
        {
            _metricService.TrackFailure("UpdateStrategy", ex);
            throw;
        }
    }

    // Remove a strategy
    public void RemoveStrategy(string strategyName)
    {
        using var tracker = _metricService.StartTracking("RemoveStrategy");
        _strategies.TryRemove(strategyName, out _);
        _metricService.TrackSuccess("RemoveStrategy");
    }

    // Optionally, reload all strategies
    public void ReloadStrategies(string strategiesPath)
    {
        using var tracker = _metricService.StartTracking("ReloadStrategies");
        _strategiesPath = strategiesPath;
        _strategies.Clear();
        LoadStrategies(strategiesPath);
        _metricService.TrackSuccess("ReloadStrategies");
    }

    /// <summary>
    /// Try get strategy
    /// </summary>
    /// <param name="name"></param>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public bool TryGetStrategy(string name, out IStrategy strategy) =>
        _strategies.TryGetValue(name, out strategy);

    public Dictionary<string, IStrategy> GetCompiledStrategies() => _strategies.ToDictionary();

    public ActionStrategy Evaluate(List<Quote> quotes)
    {
        using var tracker = _metricService.StartTracking("Evaluate");
        ConcurrentDictionary<IStrategy, ActionStrategy> results = new ConcurrentDictionary<IStrategy, ActionStrategy>();

        var result = Parallel.ForEach(GetCompiledStrategies(), (kstrategy) =>
        {
            IStrategy strategy = kstrategy.Value as IStrategy;
            var strategyResult = strategy.Execute(quotes);
            results.TryAdd(strategy, strategyResult);
        });

        if (result.IsCompleted)
        {
            Dictionary<ActionStrategy, int> scores = new();

            foreach (var item in results)
            {
                var points = item.Key.Priority + (int)item.Key.StrategyType;

                if (scores.ContainsKey(item.Value))
                {
                    scores[item.Value] += points;
                }
                else
                {
                    scores.Add(item.Value, points);
                }
            }

            _metricService.TrackSuccess("Evaluate");
            return scores.OrderByDescending(i => i.Value).FirstOrDefault().Key;
        }

        _metricService.TrackFailure("Evaluate");
        return ActionStrategy.Error;
    }

    public void UpdateStrategy(string strategyName, IStrategy strategy)
    {
        using var tracker = _metricService.StartTracking("UpdateStrategyByName");
        if (_strategies.ContainsKey(strategyName))
            _strategies[strategyName] = strategy;
        else
            _strategies.TryAdd(strategyName, strategy);

        _metricService.TrackSuccess("UpdateStrategyByName");
    }

    public void AddStrategy(string strategyName, IStrategy strategy)
    {
        using var tracker = _metricService.StartTracking("AddStrategyByName");
        _strategies.TryAdd(strategyName, strategy);
        _metricService.TrackSuccess("AddStrategyByName");
    }

    public ConcurrentDictionary<string, IStrategy> Strategies => _strategies;
}
