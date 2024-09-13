using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Datacute.EmbeddedResourcePropertyGenerator;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EmbeddedResourcePropertyGenerator.Tests;

public static class TestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(List<AdditionalText>? additionalTexts, params string[] source)
        where T : IIncrementalGenerator, new()
    {
        var (diagnostics, trees) = GetGeneratedTrees<T, TrackingNames>(additionalTexts, source);
        return (diagnostics, trees.LastOrDefault() ?? string.Empty);
    }

    public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<TGenerator, TTrackingNames>(List<AdditionalText>? additionalTexts, params string[] sources)
        where TGenerator : IIncrementalGenerator, new()
    {
        // get all the const string fields
        var trackingNames = typeof(TTrackingNames)
            .GetFields()
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(x => (string?)x.GetRawConstantValue()!)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        return GetGeneratedTrees<TGenerator>(additionalTexts, sources, trackingNames);
    }

        
    // You call this method passing in C# sources, and the list of stages you expect
    // It runs the generator, asserts the outputs are ok, 
    public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<T>(
        List<AdditionalText>? additionalTexts,
        string[] sources, // C# source code 
        string[] stages,  // The tracking stages we expect
        bool assertOutputs = true) // You can disable cacheability checking during dev
        where T : IIncrementalGenerator, new() // T is your generator
    {
        // Convert the source files to SyntaxTrees
        IEnumerable<SyntaxTree> syntaxTrees = sources.Select(static (x, i) => 
            CSharpSyntaxTree.ParseText(x)
                .WithFilePath(@$"E:\EmbeddedResourcePropertyGenerator.Tests\Test{i}.cs")
        );

        // Configure the assembly references you need
        // This will vary depending on your generator and requirements
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[] { MetadataReference.CreateFromFile(typeof(T).Assembly.Location) });

        // Create a Compilation object
        // You may want to specify other results here
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Tests",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run the generator, get the results, and assert cacheability if applicable
        GeneratorDriverRunResult runResult = RunGeneratorAndAssertOutput<T>(
            additionalTexts, compilation, stages, assertOutputs);

        // Return the generator diagnostics and generated sources
        return (runResult.Diagnostics, runResult.GeneratedTrees.Select(x => x.ToString()).ToArray());
    }
    
    private static GeneratorDriverRunResult RunGeneratorAndAssertOutput<T>(
        List<AdditionalText>? additionalTexts,
        CSharpCompilation compilation, 
        string[] trackingNames, 
        bool assertOutput = true)
        where T : IIncrementalGenerator, new()
    {
        ISourceGenerator generator = new T().AsSourceGenerator();

        var opts = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator], driverOptions: opts);

        if (additionalTexts != null)
        {
            driver = driver.AddAdditionalTexts(additionalTexts.ToImmutableArray());
        }
        var newOptions = new TestConfigOptionsProvider();
        driver = driver.WithUpdatedAnalyzerConfigOptions(newOptions);
        
        var clone = compilation.Clone();
        // Run twice, once with a clone of the compilation
        // Note that we store the returned drive value, as it contains cached previous outputs
        driver = driver.RunGenerators(compilation);
        GeneratorDriverRunResult runResult = driver.GetRunResult();

        if (assertOutput)
        {
            // Run with a clone of the compilation
            GeneratorDriverRunResult runResult2 = driver
                .RunGenerators(clone)
                .GetRunResult();

            AssertRunsEqual(runResult, runResult2, trackingNames);
            
            // verify the second run only generated cached source outputs
            runResult2.Results[0]
                .TrackedOutputSteps
                .SelectMany(x => x.Value) // step executions
                .SelectMany(x => x.Outputs) // execution results
                .Should()
                .OnlyContain(x => x.Reason == IncrementalStepRunReason.Cached);
        }

        return runResult;
    }
    
    private static void AssertRunsEqual(GeneratorDriverRunResult runResult1, GeneratorDriverRunResult runResult2, string[] trackingNames)
    {
        // We're given all the tracking names, but not all the stages have necessarily executed so filter
        Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps1 = GetTrackedSteps(runResult1, trackingNames);
        Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps2 = GetTrackedSteps(runResult2, trackingNames);

        // These should be the same
        trackedSteps1.Should()
            .NotBeEmpty()
            .And.HaveSameCount(trackedSteps2)
            .And.ContainKeys(trackedSteps2.Keys);

        foreach (var trackedStep in trackedSteps1)
        {
            var trackingName = trackedStep.Key;
            var runSteps1 = trackedStep.Value;
            var runSteps2 = trackedSteps2[trackingName];
            AssertEqual(runSteps1, runSteps2, trackingName);
        }

        return;

        static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(
            GeneratorDriverRunResult runResult, string[] trackingNames) =>
            runResult.Results[0]
                .TrackedSteps
                .Where(step => trackingNames.Contains(step.Key))
                .ToDictionary(x => x.Key, x => x.Value);
    }

    private static void AssertEqual(
        ImmutableArray<IncrementalGeneratorRunStep> runSteps1,
        ImmutableArray<IncrementalGeneratorRunStep> runSteps2,
        string stepName)
    {
        runSteps1.Should().HaveSameCount(runSteps2);

        for (var i = 0; i < runSteps1.Length; i++)
        {
            var runStep1 = runSteps1[i];
            var runStep2 = runSteps2[i];

            // The outputs should be equal between different runs
            IEnumerable<object> outputs1 = runStep1.Outputs.Select(x => x.Value);
            IEnumerable<object> outputs2 = runStep2.Outputs.Select(x => x.Value);

            outputs1.Should()
                .Equal(outputs2, $"because {stepName} should produce cacheable outputs");

            // Therefore, on the second run the results should always be cached or unchanged!
            // - Unchanged is when the _input_ has changed, but the output hasn't
            // - Cached is when the the input has not changed, so the cached output is used 
            runStep2.Outputs.Should()
                .OnlyContain(
                    x => x.Reason == IncrementalStepRunReason.Cached || x.Reason == IncrementalStepRunReason.Unchanged,
                    $"{stepName} expected to have reason {IncrementalStepRunReason.Cached} or {IncrementalStepRunReason.Unchanged}");

            // Make sure we're not using anything we shouldn't
            AssertObjectGraph(runStep1, stepName);
            AssertObjectGraph(runStep2, stepName);
        }

        static void AssertObjectGraph(IncrementalGeneratorRunStep runStep, string stepName)
        {
            var because = $"{stepName} shouldn't contain banned symbols";
            var visited = new HashSet<object>();

            foreach (var (obj, _) in runStep.Outputs)
            {
                Visit(obj);
            }

            void Visit(object? node)
            {
                if (node is null || !visited.Add(node))
                {
                    return;
                }

                node.Should()
                    .NotBeOfType<Compilation>(because)
                    .And.NotBeOfType<ISymbol>(because)
                    .And.NotBeOfType<SyntaxNode>(because);

                Type type = node.GetType();
                if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                {
                    return;
                }

                if (node is IEnumerable collection and not string)
                {
                    foreach (object element in collection)
                    {
                        Visit(element);
                    }

                    return;
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                           BindingFlags.Instance))
                {
                    object? fieldValue = field.GetValue(node);
                    Visit(fieldValue);
                }
            }
        }
    }

    public static Task Verify(string source, List<AdditionalText>? additionalTexts = null)
    {
        // Create references for assemblies we require
        // We could add multiple references if required
        IEnumerable<PortableExecutableReference> references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };
        
        // Parse the provided string into a C# syntax tree
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source)
            .WithFilePath(@"E:\EmbeddedResourcePropertyGenerator.Tests\Test.cs");

        // Create a Roslyn compilation for the syntax tree.
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests")
            .WithReferences(references)
            .AddSyntaxTrees(syntaxTree);


        // Create an instance of our Embedded Resource Property incremental source generator
        var generator = new Generator();
        
        

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        if (additionalTexts != null)
        {
            driver = driver.AddAdditionalTexts(additionalTexts.ToImmutableArray());
        }

        var newOptions = new TestConfigOptionsProvider();
        driver = driver.WithUpdatedAnalyzerConfigOptions(newOptions);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver);
    }
}