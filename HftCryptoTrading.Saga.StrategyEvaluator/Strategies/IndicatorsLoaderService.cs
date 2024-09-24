using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using Skender.Stock.Indicators;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Indicators;

public class IndicatorLoaderService
{
    private string _indicatorsPath;
    private ConcurrentDictionary<string, IIndicator> _indicators;
    private static Lazy<IndicatorLoaderService> _indicatorLoaderService =
        new Lazy<IndicatorLoaderService>(new IndicatorLoaderService());

    private IndicatorLoaderService()
    {
        _indicators = new ConcurrentDictionary<string, IIndicator>(); // Update to store IIndicator
    }

    public static IndicatorLoaderService Service => _indicatorLoaderService.Value;

    public void LoadIndicators(string indicatorsPath)
    {
        _indicatorsPath = indicatorsPath;
        LoadIndicators();
    }

    // Load indicators at the start
    private void LoadIndicators()
    {
        if (!Directory.Exists(_indicatorsPath))
        {
            throw new DirectoryNotFoundException($"The specified indicators directory does not exist: {_indicatorsPath}");
        }

        var indicatorFiles = Directory.GetFiles(_indicatorsPath, "*.cs");

        foreach (var file in indicatorFiles)
        {
            CompileIndicator(file);
        }
    }

    // Compile and add indicator from file
    private void CompileIndicator(string filePath)
    {
        var code = File.ReadAllText(filePath);

        var usingDirectives = @"
        //added automatically by the generator
        using System;
        using Skender.Stock.Indicators;
        using System.Collections.Generic;
        using System.Linq;
        using HftCryptoTrading.Shared.Strategies;
        //end added automatically by the generator
        ";

        var @namespace = new StringBuilder("HftCryptoTrading.CustomIndicators");
        @namespace.Append(Path.GetDirectoryName(filePath).Replace(Path.DirectorySeparatorChar, '.'));

        var fullcode = new StringBuilder();

        fullcode.AppendLine(usingDirectives);
        fullcode.AppendLine();
        fullcode.AppendLine($"namespace {@namespace.ToString()};");
        fullcode.AppendLine();

        fullcode.AppendLine(code);

        var syntaxTree = CSharpSyntaxTree.ParseText(fullcode.ToString());

        // Utiliser une expression régulière pour extraire le nom de la classe
        var classNameMatch = Regex.Match(code, @"class\s+(\w+)");
        string indicatorName;

        if (classNameMatch.Success)
        {
            indicatorName = classNameMatch.Groups[1].Value; // Le nom de la classe
        }
        else
        {
            throw new InvalidOperationException($"No class found in {filePath}");
        }

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
            var type = assembly.GetType($"{@namespace.ToString()}.{indicatorName}"); // Utilisation du namespace et du nom de la classe

            if (type.GetInterface(nameof(IIndicator)) == null)
            {
                throw new InvalidOperationException($"Indicator {indicatorName} does not implement IIndicator.");
            }

            // Create an instance of the indicator (no parameters in the constructor)
            var indicatorInstance = (IIndicator)Activator.CreateInstance(type);

            // Store the indicator in the dictionary
            _indicators[indicatorName] = indicatorInstance;
        }
    }

    // Get necessary references
    private List<MetadataReference> GetReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var references = new List<MetadataReference>();

        foreach (var assembly in assemblies)
        {
            // Vérifie si l'assembly a un chemin valide et n'est pas dynamique
            if (!string.IsNullOrEmpty(assembly.Location) && !assembly.IsDynamic)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        return references;
    }

    // Execute a indicator
    public IEnumerable<decimal> ExecuteIndicator(string indicatorName, IEnumerable<Quote> quotes, params object[] parameters)
    {
        if (_indicators.TryGetValue(indicatorName, out var indicator))
        {
            return indicator.Execute(quotes, parameters); // Call the Execute method on IIndicator
        }

        throw new KeyNotFoundException($"Indicator '{indicatorName}' not found.");
    }


    // Add a new indicator
    public void AddIndicator(string filePath)
    {
        if (File.Exists(filePath))
        {
            CompileIndicator(filePath);
        }
        else
        {
            throw new FileNotFoundException($"Indicator file not found: {filePath}");
        }
    }

    // Update an existing indicator
    public void UpdateIndicator(string filePath)
    {
        // Remove the indicator first
        var indicatorName = Path.GetFileNameWithoutExtension(filePath);
        RemoveIndicator(indicatorName);

        // Compile and add the updated indicator
        CompileIndicator(filePath);
    }

    // Remove a indicator
    public void RemoveIndicator(string indicatorName)
    {
        _indicators.TryRemove(indicatorName, out _);
    }

    // Optionally, reload all indicators
    public void ReloadIndicators(string indicatorsPath)
    {
        _indicatorsPath = indicatorsPath;
        _indicators.Clear();
        LoadIndicators();
    }

    public ConcurrentDictionary<string, IIndicator> GetCompiledIndicators() => _indicators;
}