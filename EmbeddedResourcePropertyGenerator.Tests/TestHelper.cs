using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EmbeddedResourcePropertyGenerator.Tests;

public static class TestHelper
{
    public const string TestNamespace = "EmbeddedResourcePropertyGenerator.Tests";
    public const string TestPath = @"E:\EmbeddedResourcePropertyGenerator.Tests\Tests";
    
    public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedOutput<TAttribute, TGenerator, TTrackingNames>(
        List<AdditionalText>? additionalTexts, 
        params string[] sources)
        where TAttribute : Attribute
        where TGenerator : IIncrementalGenerator, new()
    {
        // get all the const string fields
        var trackingNames = typeof(TTrackingNames)
            .GetFields()
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(x => (string?)x.GetRawConstantValue()!)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        var compilation = GetCompilation<TAttribute, TGenerator>(sources);

        // Run the generator, get the results, and assert cacheability if applicable
        GeneratorDriverRunResult runResult = RunGeneratorAndAssertOutput<TGenerator>(
            additionalTexts, compilation, trackingNames);

        // Return the generator diagnostics and generated sources
        return (runResult.Diagnostics, runResult.GeneratedTrees.Select(x => x.ToString()).ToArray());
    }

    private static CSharpCompilation GetCompilation<TAttribute, TGenerator>(params string[] sources)
        where TAttribute : Attribute
        where TGenerator : IIncrementalGenerator, new()
    {
        // Convert the source files to SyntaxTrees
        var syntaxTrees = sources.Select(static (s, i) => 
            CSharpSyntaxTree.ParseText(s).WithFilePath(@$"{TestPath}\Test{i}.cs")
        );

        // Configure the assembly references you need
        // This will vary depending on your generator and requirements
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(TGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(TAttribute).Assembly.Location)
            });

        // Create a Compilation object
        // You may want to specify other results here
        return CSharpCompilation.Create("Tests", syntaxTrees, references);
    }

    private static GeneratorDriverRunResult RunGeneratorAndAssertOutput<TGenerator>(
        List<AdditionalText>? additionalTexts,
        CSharpCompilation compilation, 
        string[] trackingNames, 
        bool assertOutput = true)
        where TGenerator : IIncrementalGenerator, new()
    {
        var driver = GetDriver<TGenerator>(additionalTexts);

        var clone = compilation.Clone();

        // Run twice, once with a clone of the compilation
        // Note that we store the returned drive value, as it contains cached previous outputs
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        if (!assertOutput) return runResult;

        // Run with a clone of the compilation
        var runResult2 = driver
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

        return runResult;
    }

    private static GeneratorDriver GetDriver<TGenerator>(List<AdditionalText>? additionalTexts)
        where TGenerator : IIncrementalGenerator, new()
    {
        var generator = new TGenerator().AsSourceGenerator();

        var testConfigOptionsProvider = new TestConfigOptionsProvider();

        var generatorDriverOptions = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);

        return CSharpGeneratorDriver.Create(
            [generator], 
            additionalTexts, 
            optionsProvider: testConfigOptionsProvider,
            driverOptions: generatorDriverOptions);
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
    }

    private static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(GeneratorDriverRunResult runResult, string[] trackingNames) =>
        runResult.Results[0]
            .TrackedSteps
            .Where(step => trackingNames.Contains(step.Key))
            .ToDictionary(x => x.Key, x => x.Value);

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
            // - Unchanged is when the input has changed, but the output hasn't
            // - Cached is when the input has not changed, so the cached output is used 
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

    public static Task Verify<TAttribute, TGenerator>(string source, List<AdditionalText>? additionalTexts = null)
        where TAttribute : Attribute
        where TGenerator : IIncrementalGenerator, new()
    {
        var driver = GetDriver<TGenerator>(additionalTexts);
        var compilation = GetCompilation<TAttribute, TGenerator>(source);
        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }
}