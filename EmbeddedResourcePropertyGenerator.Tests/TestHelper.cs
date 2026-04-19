using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace EmbeddedResourcePropertyGenerator.Tests;

public static class TestHelper
{
    public const string TestNamespace = "EmbeddedResourcePropertyGenerator.Tests";

    public static string TestPath(string path) => 
        Path.GetFullPath($"/EmbeddedResourcePropertyGenerator.Tests/Tests/{path}"
            .Replace('/', Path.DirectorySeparatorChar));

    public static (GeneratorDriver, CSharpCompilation)
        NoModification(
            GeneratorDriver driver,
            CSharpCompilation compilation)
        => (driver, compilation);
    
    public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output1, string[] Output2)
        GetGeneratedOutput<TGenerator>(
            Func<GeneratorDriver, CSharpCompilation, (GeneratorDriver, CSharpCompilation)> scenarioModification,
            List<AdditionalText>? additionalTexts,
            string[] trackingNamesToVerifyUnchanged,
            Func<GeneratorDriver, CSharpCompilation, (GeneratorDriver, CSharpCompilation)> modificationBetweenRuns,
            params string[] sources)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = GetCompilation<TGenerator>(LanguageVersion.CSharp13, sources);

        // Run the generator, get the results, and assert cacheability if applicable
        (GeneratorDriverRunResult runResult1, GeneratorDriverRunResult runResult2) =
            RunGeneratorAndAssertOutput<TGenerator>(
                scenarioModification,
                additionalTexts,
                compilation,
                trackingNamesToVerifyUnchanged,
                modificationBetweenRuns,
                LanguageVersion.CSharp13);

        // Return the generator diagnostics and generated sources
        return (runResult1.Diagnostics,
            runResult1.GeneratedTrees.Select(x => x.ToString()).ToArray(),
            runResult2.GeneratedTrees.Select(x => x.ToString()).ToArray());
    }

    private static CSharpCompilation GetCompilation<TGenerator>(LanguageVersion languageVersion, params string[] sources)
        where TGenerator : IIncrementalGenerator, new()
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        // Convert the source files to SyntaxTrees
        var syntaxTrees = sources.Select((s, i) =>
            CSharpSyntaxTree.ParseText(s, parseOptions).WithFilePath(TestPath($"Test{i}.cs"))
        );

        // Configure the assembly references you need
        // This will vary depending on your generator and requirements
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat([
                MetadataReference.CreateFromFile(typeof(TGenerator).Assembly.Location)
            ]);

        // Create a Compilation object
        // You may want to specify other results here
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable);
        return CSharpCompilation.Create("Tests", syntaxTrees, references, compilationOptions);
    }

    private static (GeneratorDriverRunResult, GeneratorDriverRunResult) RunGeneratorAndAssertOutput<TGenerator>(
        Func<GeneratorDriver, CSharpCompilation, (GeneratorDriver, CSharpCompilation)> scenarioModification,
        List<AdditionalText>? additionalTexts,
        CSharpCompilation compilation,
        string[] trackingNamesToVerifyUnchanged,
        Func<GeneratorDriver, CSharpCompilation, (GeneratorDriver, CSharpCompilation)> modificationBetweenRuns,
        LanguageVersion languageVersion)
        where TGenerator : IIncrementalGenerator, new()
    {
        var driver = GetDriver<TGenerator>(additionalTexts, languageVersion);

        (driver, compilation) = scenarioModification(driver, compilation);

        var clone = compilation.Clone();

        // Run twice, once with a clone of the compilation
        // Note that we store the returned drive value, as it contains cached previous outputs
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        // If a modification between runs is specified, apply it
        (driver, clone) = modificationBetweenRuns(driver, clone);

        // Run with a clone of the compilation
        var runResult2 = driver
            .RunGenerators(clone)
            .GetRunResult();

        AssertRunsEqual(runResult, runResult2, trackingNamesToVerifyUnchanged);
            
        // verify the second run only generated cached source outputs
        runResult2.Results[0]
            .TrackedOutputSteps
            .Where(step => trackingNamesToVerifyUnchanged.Contains(step.Key))
            .SelectMany(x => x.Value) // step executions
            .SelectMany(x => x.Outputs) // execution results
            .ShouldAllBe(x => x.Reason == IncrementalStepRunReason.Cached);

        return (runResult, runResult2);
    }

    private static GeneratorDriver GetDriver<TGenerator>(List<AdditionalText>? additionalTexts, LanguageVersion languageVersion)
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
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion),
            optionsProvider: testConfigOptionsProvider,
            driverOptions: generatorDriverOptions);
    }

    private static void AssertRunsEqual(
        GeneratorDriverRunResult runResult1, 
        GeneratorDriverRunResult runResult2,
        string[] trackingNamesToVerifyUnchanged)
    {
        // We're given all the tracking names, but not all the stages have necessarily executed so filter
        Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps1 = GetTrackedSteps(runResult1, trackingNamesToVerifyUnchanged);
        Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps2 = GetTrackedSteps(runResult2, trackingNamesToVerifyUnchanged);

        // These should be the same
        //trackedSteps1.Count.ShouldBe(trackingNamesToVerifyUnchanged.Length);
        //trackedSteps1.Count.ShouldBe(trackedSteps2.Count);

        if (trackingNamesToVerifyUnchanged.Length > 0)
        {
            trackedSteps1.Keys.ShouldBeEquivalentTo(trackedSteps2.Keys);
        }

        foreach (var trackedStep in trackedSteps1)
        {
            var trackingName = trackedStep.Key;
            var runSteps1 = trackedStep.Value;
            var runSteps2 = trackedSteps2[trackingName];
            AssertEqual(runSteps1, runSteps2, trackingName);
        }
    }

    private static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(
        GeneratorDriverRunResult runResult, 
        string[] trackingNamesToVerifyUnchanged
        ) =>
        runResult.Results[0]
            .TrackedSteps
            .Where(step => trackingNamesToVerifyUnchanged.Contains(step.Key))
            .ToDictionary(x => x.Key, x => x.Value);

    private static void AssertEqual(
        ImmutableArray<IncrementalGeneratorRunStep> runSteps1,
        ImmutableArray<IncrementalGeneratorRunStep> runSteps2,
        string stepName)
    {
        runSteps1.Length.ShouldBe(runSteps2.Length);

        for (var i = 0; i < runSteps1.Length; i++)
        {
            var runStep1 = runSteps1[i];
            var runStep2 = runSteps2[i];

            // The outputs should be equal between different runs
            IEnumerable<object> outputs1 = runStep1.Outputs.Select(x => x.Value);
            IEnumerable<object> outputs2 = runStep2.Outputs.Select(x => x.Value);

            outputs1.ShouldBeEquivalentTo(outputs2, $"because {stepName} should produce cacheable outputs");

            // Therefore, on the second run the results should always be cached or unchanged!
            // - Unchanged is when the input has changed, but the output hasn't
            // - Cached is when the input has not changed, so the cached output is used 
            runStep2.Outputs.ShouldAllBe(
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

                node.ShouldNotBeOfType<Compilation>(because);
                node.ShouldNotBeOfType<ISymbol>(because);
                node.ShouldNotBeOfType<SyntaxNode>(because);

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

    public static Task Verify<TGenerator>(string source, List<AdditionalText>? additionalTexts = null, LanguageVersion languageVersion = LanguageVersion.CSharp13)
        where TGenerator : IIncrementalGenerator, new()
    {
        var driver = GetDriver<TGenerator>(additionalTexts, languageVersion);
        var compilation = GetCompilation<TGenerator>(languageVersion, source);
        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver)
            .ScrubLinesWithReplace(line => 
                Regex.Replace(line, @"Version: \d+\.\d+\.\d+", "Version: 1.2.3"))
            .IgnoreGeneratedResult(result =>
            {
                switch (result.HintName)
                {
                    case "Datacute.EmbeddedResourcePropertyGenerator.EmbeddedResourcePropertiesAttribute.g.cs":
                    case "Microsoft.CodeAnalysis.EmbeddedAttribute.cs":
                        return true;
                    default:
                        return false;
                }
            });
    }
}