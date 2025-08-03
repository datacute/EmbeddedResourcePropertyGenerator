using Datacute.IncrementalGeneratorExtensions;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    /// <summary>
    /// Generate partial classes containing properties giving access to embedded resources.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            LightweightTrace.MethodEntry(GeneratorStage.Initialize);

            context.RegisterPostInitializationOutput(static postInitializationContext =>
            {
                postInitializationContext.AddSource(
                    Templates.AttributeHintName,
                    Templates.EmbeddedResourcePropertiesAttribute);
            });

            // 1. Base attribute data -> AttributeContextAndData<AttributeData>
            var attributeContexts =
                context.SelectAttributeContexts(
                    Templates.AttributeFullyQualified,
                    AttributeData.Collector);

            // 2. Options -> GeneratorOptions
            var options =
                context.AnalyzerConfigOptionsProvider
                    .Select(GeneratorOptions.Select)
                    .WithTrackingName(GeneratorStage.AnalyzerConfigOptionsProviderSelect);

            // 3. Combine base attributes and options -> (AttributeContext, GeneratorOptions)
            var attributesAndOptions =
                attributeContexts
                    .Combine(options)
                    .Select(SelectAttributeAndOptions)
                    .WithTrackingName(TrackingNames.AttributesAndOptionsCombined);

            // --- Prepare Resource Matching Data Separately ---

            // Selects (AttributeContext, Path, Extension) from (AttributeContext, Options)
            var attributesAndGlobs =
                attributesAndOptions
                    .Select(SelectAttributesAndGlobs)
                    .WithTrackingName(TrackingNames.AttributeGlobInfoSelected);

            // Selects (Path, Extension) from (AttributeContext, Path, Extension)
            var attributeGlobs =
                attributesAndGlobs
                    .Select(SelectJustGlobs)
                    .WithTrackingName(TrackingNames.AttributeGlobsSelected);

            // 4. Find all (AttributeContext, EmbeddedResource) matches
            var resourcesByAttributeContextLookup =
                context.AdditionalTextsProvider
                    .Select(SelectFileInfo)
                    .WithTrackingName(GeneratorStage.AdditionalTextsProviderSelect)
                    .CombineEquatable(attributeGlobs)
                    .WithTrackingName(TrackingNames.FileInfoAndGlobsCombined)
                    .Select(SelectAdditionalTextAndGlobWithAttributeGlobs)
                    .Where(DoesAdditionalTextGlobMatchAnyAttributeGlobs)
                    .WithTrackingName(TrackingNames.MatchingFilesFiltered)
                    .Select(ExtractEmbeddedResourceWithFileInfo)
                    .WithTrackingName(TrackingNames.EmbeddedResourceExtracted)
                    .CombineEquatable(attributesAndGlobs)
                    .WithTrackingName(TrackingNames.ResourceAndAllAttributeGlobsCombined)
                    .SelectMany(SelectMatchingResourceAndAttribute)
                    .WithTrackingName(TrackingNames.MatchingResourceAndAttributeSelected)
                    .CollectEquatable()
                    .Select(GroupResourcesByAttributeContext)
                    .WithTrackingName(TrackingNames.ResourcesGroupedByAttributeContext);

            // --- Combine Base Attributes with Grouped Resources (Left Join) ---

            // 6. Combine the main attributes stream with the single lookup dictionary
            var generationInput =
                attributesAndOptions
                    .Combine(resourcesByAttributeContextLookup)
                    .Select(PerformResourceLookup)
                    .WithTrackingName(TrackingNames.GenerationInputPrepared);

            // 7. Register Source Output
            context.RegisterSourceOutput(
                generationInput,
                (sourceProductionContext, inputData) =>
                {
                    LightweightTrace.Add(GeneratorStage.RegisterSourceOutput);
                    var (attributeContext, generatorOptions, embeddedResources) = inputData;
                    GenerateFolderEmbed(sourceProductionContext, attributeContext, embeddedResources, generatorOptions);
                });

            LightweightTrace.MethodExit(GeneratorStage.Initialize);
        }

        // --- Helper Methods ---

        // Selects (AdditionalText, Directory, Extension) from AdditionalText
        private static AttributeAndOptions SelectAttributeAndOptions((AttributeContextAndData<AttributeData> AttributeContext, GeneratorOptions Options) attributeAndOptions, CancellationToken _) =>
            new(attributeAndOptions.AttributeContext, attributeAndOptions.Options);

        // Selects (AttributeContext, Path, Extension) from (AttributeContext, Options)
        private static AttributeAndGlob SelectAttributesAndGlobs(AttributeAndOptions attributeAndOptions, CancellationToken ct)
        {
            var resourceSearchPath = GetResourceSearchPath(attributeAndOptions.AttributeContext.AttributeData, attributeAndOptions.Options);
            var glob = new Glob(resourceSearchPath, attributeAndOptions.AttributeContext.AttributeData.ExtensionArg);
            return new AttributeAndGlob(attributeAndOptions.AttributeContext, glob);
        }

        // Selects (Path, Extension) from (AttributeContext, Path, Extension)
        private static Glob SelectJustGlobs(AttributeAndGlob attributeAndGlob, CancellationToken _) => attributeAndGlob.Glob;

        // Selects (AdditionalText, Directory, Extension) from AdditionalText
        private static AdditionalTextAndGlob SelectFileInfo(AdditionalText additionalText, CancellationToken _) =>
            new(additionalText, new Glob(Path.GetDirectoryName(additionalText.Path), Path.GetExtension(additionalText.Path)));

        private static AdditionalTextAndGlobWithAttributeGlobs SelectAdditionalTextAndGlobWithAttributeGlobs(
            (AdditionalTextAndGlob AdditionalTextAndGlob, EquatableImmutableArray<Glob> AttributeGlobs) source,
            CancellationToken _) =>
            new(source.AdditionalTextAndGlob, source.AttributeGlobs);

        // Checks if the file's (Directory, Extension) matches any glob in the list
        private static bool DoesAdditionalTextGlobMatchAnyAttributeGlobs(
            AdditionalTextAndGlobWithAttributeGlobs additionalTextAndGlobWithAttributeGlobs)
        {
            var attributeGlobsArray = additionalTextAndGlobWithAttributeGlobs.AttributeGlobs;
            var additionalTextAndGlob = additionalTextAndGlobWithAttributeGlobs.AdditionalTextAndGlob;
            var glob = additionalTextAndGlob.Glob;
            var directory = glob.Directory;
            var extension = glob.Extension;

            return attributeGlobsArray.Any(attributeGlob => 
                directory == attributeGlob.Directory && 
                (attributeGlob.Extension == ".*" || extension == attributeGlob.Extension));
        }

        // Extracts EmbeddedResource content, keeping the original file+glob info
        private static AdditionalTextGlobsAndResources ExtractEmbeddedResourceWithFileInfo(
            AdditionalTextAndGlobWithAttributeGlobs additionalTextAndGlobWithAttributeGlobs, 
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(TrackingNames.GeneratingDocComment);

            var additionalText = additionalTextAndGlobWithAttributeGlobs.AdditionalTextAndGlob.AdditionalText;
            LightweightTrace.Add(TrackingNames.GeneratingDocComment, additionalText.Path.Length);

            var docCommentCode = AdditionalTextDocCommentCreator.GenerateDocCommentCode(additionalText, ct);
            var embeddedResource = new EmbeddedResource(additionalText.Path, docCommentCode);

            return new AdditionalTextGlobsAndResources(additionalTextAndGlobWithAttributeGlobs, embeddedResource);
        }

        // Creates (AttributeContext, EmbeddedResource) for each matching attribute
        private static IEnumerable<AttributeAndResource> SelectMatchingResourceAndAttribute(
                (
                    AdditionalTextGlobsAndResources ResourceAndFileData,
                    EquatableImmutableArray<AttributeAndGlob> AttributesAndGlobs
                ) resourceAndAllAttributeGlobs,
                CancellationToken ct)
        {
            var resourceAndFileData = resourceAndAllAttributeGlobs.ResourceAndFileData;
            var allAttributesAndGlobsArray = resourceAndAllAttributeGlobs.AttributesAndGlobs;
            var glob = resourceAndFileData.FileAndGlobs.AdditionalTextAndGlob.Glob;
            var directory = glob.Directory;
            var extension = glob.Extension;
            var embeddedResource = resourceAndFileData.EmbeddedResource;

            return allAttributesAndGlobsArray
                .Where(attrAndGlob => 
                    directory == attrAndGlob.Glob.Directory && 
                    (attrAndGlob.Glob.Extension == ".*" || extension == attrAndGlob.Glob.Extension))
                .Select(matchingAttrAndGlob => new AttributeAndResource(matchingAttrAndGlob.AttributeContext, embeddedResource));
        }

        // Groups the collected (AttributeContext, EmbeddedResource) into a dictionary lookup
        private static Dictionary<AttributeContextAndData<AttributeData>, EquatableImmutableArray<EmbeddedResource>> GroupResourcesByAttributeContext(
            EquatableImmutableArray<AttributeAndResource> resourceAndAttribute, CancellationToken ct)
        {
            var grouped = new Dictionary<AttributeContextAndData<AttributeData>, ImmutableArray<EmbeddedResource>.Builder>();
            foreach (var (context, resource) in resourceAndAttribute)
            {
                ct.ThrowIfCancellationRequested();
                if (!grouped.TryGetValue(context, out var builder))
                {
                    builder = ImmutableArray.CreateBuilder<EmbeddedResource>();
                    grouped[context] = builder;
                }
                builder.Add(resource);
            }

            return grouped.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutable().ToEquatableImmutableArray(ct));
        }

        // Performs the left join lookup for resources based on the AttributeContext
        private static AttributeOptionsAndResources PerformResourceLookup(
            (
                AttributeAndOptions AttributeAndOptions,
                Dictionary<AttributeContextAndData<AttributeData>, EquatableImmutableArray<EmbeddedResource>> ResourcesByAttributeContextLookup
            ) attributeOptionsAndLookup,
            CancellationToken _)
        {
            var attributeContext = attributeOptionsAndLookup.AttributeAndOptions.AttributeContext;
            var options = attributeOptionsAndLookup.AttributeAndOptions.Options;
            var lookupDictionary = attributeOptionsAndLookup.ResourcesByAttributeContextLookup;

            var resourcesArray = lookupDictionary.TryGetValue(attributeContext, out var foundResourcesArray)
                ? foundResourcesArray
                : EquatableImmutableArray<EmbeddedResource>.Empty;

            return new AttributeOptionsAndResources(attributeContext, options, resourcesArray);
        }

        private static void GenerateFolderEmbed(
            in SourceProductionContext context,
            in AttributeContextAndData<AttributeData> attributeContextAndData,
            EquatableImmutableArray<EmbeddedResource> embeddedResources,
            in GeneratorOptions options)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested(GeneratorStage.SourceProductionContextAddSource);

            var resourceSearchPath = GetResourceSearchPath(attributeContextAndData.AttributeData, options);

            var codeGenerator = new CodeGenerator(
                attributeContextAndData,
                resourceSearchPath,
                embeddedResources,
                options,
                cancellationToken);

            var hintName = attributeContextAndData.CreateHintName("EmbeddedResourceProperties");
            var source = codeGenerator.GetSourceText();
            LightweightTrace.Add(GeneratorStage.SourceProductionContextAddSource);
            context.AddSource(hintName, source);
        }

        private static string GetResourceSearchPath(in AttributeData attributeData, in GeneratorOptions options)
        {
            string baseDir;
            string resourceSearchPath;

            var pathArg = attributeData.PathArg;

            if (pathArg.StartsWith("/") || pathArg.StartsWith("\\"))
            {
                baseDir = options.ProjectDir;
                resourceSearchPath = pathArg.Length > 0 ? pathArg.Substring(1) : string.Empty;
            }
            else
            {
                baseDir = Path.GetDirectoryName(attributeData.FilePath) ?? string.Empty;
                resourceSearchPath = pathArg;
            }

            try
            {
                return Path.GetFullPath(Path.Combine(baseDir, resourceSearchPath));
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }
    }

    public record struct AttributeAndOptions(AttributeContextAndData<AttributeData> AttributeContext, GeneratorOptions Options);
    public record struct Glob(string? Directory, string Extension);
    public record struct AttributeAndGlob(AttributeContextAndData<AttributeData> AttributeContext, Glob Glob);
    public record struct AdditionalTextAndGlob(AdditionalText AdditionalText, Glob Glob);
    public record struct AdditionalTextGlobsAndResources(AdditionalTextAndGlobWithAttributeGlobs FileAndGlobs, EmbeddedResource EmbeddedResource);
    public record struct AdditionalTextAndGlobWithAttributeGlobs(AdditionalTextAndGlob AdditionalTextAndGlob, EquatableImmutableArray<Glob> AttributeGlobs);
    public record struct AttributeAndResource(AttributeContextAndData<AttributeData> AttributeContext, EmbeddedResource Resource);
    public record struct AttributeOptionsAndResources(AttributeContextAndData<AttributeData> Context, GeneratorOptions Options, EquatableImmutableArray<EmbeddedResource> Resources);
}
