using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Skender.Stock.Indicators;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public class StrategyLoaderService
{
    private readonly string _strategiesPath;
    private readonly ConcurrentDictionary<string, IStrategy> _strategies;

    public StrategyLoaderService(string strategiesPath)
    {
        _strategiesPath = strategiesPath;
        _strategies = new ConcurrentDictionary<string, IStrategy>(); // Update to store IStrategy
        LoadStrategies();
    }

    // Load strategies at the start
    private void LoadStrategies()
    {
        if (!Directory.Exists(_strategiesPath))
        {
            throw new DirectoryNotFoundException($"The specified strategies directory does not exist: {_strategiesPath}");
        }

        var strategyFiles = Directory.GetFiles(_strategiesPath, "*.cs");

        foreach (var file in strategyFiles)
        {
            CompileStrategy(file);
        }
    }

    // Compile and add strategy from file
    private void CompileStrategy(string filePath)
    {
        var code = File.ReadAllText(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var strategyName = Path.GetFileNameWithoutExtension(filePath);

        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(filePath),
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        throw new InvalidOperationException($"Error compiling {filePath}: {diagnostic.GetMessage()}");
                    }
                }
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var type = assembly.GetType(strategyName); // Adjust according to the class name in the strategy file

            if (type.GetInterface(nameof(IStrategy)) == null)
            {
                throw new InvalidOperationException($"Strategy {strategyName} does not implement IStrategy.");
            }

            // Create an instance of the strategy (no parameters in the constructor)
            var strategyInstance = (IStrategy)Activator.CreateInstance(type);

            // Store the strategy in the dictionary
            _strategies[strategyName] = strategyInstance;
        }
    }

    // Get necessary references
    private List<MetadataReference> GetReferences()
    {
        return new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Quote).Assembly.Location),
        };
    }

    // Execute a strategy
    public ActionStrategy ExecuteStrategy(string strategyName, IEnumerable<Quote> quotes)
    {
        if (_strategies.TryGetValue(strategyName, out var strategy))
        {
            var result = strategy.Execute(); // Call the Execute method on IStrategy
    
            if(result == ActionStrategy.Error)
                Console.WriteLine(strategy.Error);

            return result;
        }

        throw new KeyNotFoundException($"Strategy '{strategyName}' not found.");
    }


    // Add a new strategy
    public void AddStrategy(string filePath)
    {
        if (File.Exists(filePath))
        {
            CompileStrategy(filePath);
        }
        else
        {
            throw new FileNotFoundException($"Strategy file not found: {filePath}");
        }
    }

    // Update an existing strategy
    public void UpdateStrategy(string filePath)
    {
        // Remove the strategy first
        var strategyName = Path.GetFileNameWithoutExtension(filePath);
        RemoveStrategy(strategyName);

        // Compile and add the updated strategy
        CompileStrategy(filePath);
    }

    // Remove a strategy
    public void RemoveStrategy(string strategyName)
    {
        _strategies.TryRemove(strategyName, out _);
    }

    // Optionally, reload all strategies
    public void ReloadStrategies()
    {
        _strategies.Clear();
        LoadStrategies();
    }

    public ConcurrentDictionary<string, IStrategy> GetCompiledStrategies() => _strategies;
}