using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            LightweightTrace.Add((int)TrackingNames.GeneratorInitialized);

            // 1. Base attribute data -> AttributeContext
            var attributeContexts =
                context.SyntaxProvider
                    .ForAttributeWithMetadataName(
                        Templates.AttributeFullyQualified,
                        predicate: (node, _) => node is TypeDeclarationSyntax,
                        transform: (attributeSyntaxContext, _) => new AttributeContext(attributeSyntaxContext))
                    .Trace(TrackingNames.AttributeContextsCreated);

            // 2. Options -> GeneratorOptions
            var options =
                context.AnalyzerConfigOptionsProvider
                    .Select(GeneratorOptions.Select)
                    .Trace(TrackingNames.AnalyzerConfigOptions);

            // 3. Combine base attributes and options -> (AttributeContext, GeneratorOptions)
            var attributesAndOptions =
                attributeContexts
                    .Combine(options)
                    .Select(SelectAttributeAndOptions)
                    .Trace(TrackingNames.AttributesAndOptionsCombined);

            // --- Prepare Resource Matching Data Separately ---

            // Selects (AttributeContext, Path, Extension) from (AttributeContext, Options)
            var attributesAndGlobs =
                attributesAndOptions
                    .Select(SelectAttributesAndGlobs)
                    .Trace(TrackingNames.AttributeGlobInfoSelected);

            // Selects (Path, Extension) from (AttributeContext, Path, Extension)
            var attributeGlobs =
                attributesAndGlobs
                    .Select(SelectJustGlobs)
                    .Trace(TrackingNames.AttributeGlobsSelected);

            // 4. Find all (AttributeContext, EmbeddedResource) matches
            var matchedResourceAndAttribute =
                context.AdditionalTextsProvider
                    .Select(SelectFileInfo)
                    .Trace(TrackingNames.FileInfoSelected)
                    .CombineEquatable(attributeGlobs)
                    .Trace(TrackingNames.FileInfoAndGlobsCombined)
                    .Select(SelectAdditionalTextAndGlobWithAttributeGlobs)
                    .Where(DoesAdditionalTextGlobMatchAnyAttributeGlobs)
                    .Trace(TrackingNames.MatchingFilesFiltered)
                    .Select(ExtractEmbeddedResourceWithFileInfo)
                    .Trace(TrackingNames.EmbeddedResourceExtracted)
                    .CombineEquatable(attributesAndGlobs)
                    .Trace(TrackingNames.ResourceAndAllAttributeGlobsCombined)
                    .SelectMany(SelectMatchingResourceAndAttribute)
                    .Trace(TrackingNames.MatchingResourceAndAttributeSelected);

            // 5. Group resources by AttributeContext into a lookup
            var resourcesByAttributeContextLookup =
                matchedResourceAndAttribute
                    .Collect().Select(EquatableImmutableArray<AttributeAndResource>.Create)
                    .Select(GroupResourcesByAttributeContext)
                    .Trace(TrackingNames.ResourcesGroupedByAttributeContext);

            // --- Combine Base Attributes with Grouped Resources (Left Join) ---

            // 6. Combine the main attributes stream with the single lookup dictionary
            var generationInput =
                attributesAndOptions
                    .Combine(resourcesByAttributeContextLookup)
                    .Select(PerformResourceLookup)
                    .Trace(TrackingNames.GenerationInputPrepared);

            // 7. Register Source Output
            context.RegisterSourceOutput(generationInput,
                (sourceProductionContext, inputData) =>
                {
                    LightweightTrace.Add((int)TrackingNames.GeneratingSourceFile);
                    var (attributeContext, generatorOptions, embeddedResources) = inputData;
                    GenerateFolderEmbed(sourceProductionContext, attributeContext, embeddedResources, generatorOptions);
                });
        }

        // --- Helper Methods ---

        // Selects (AdditionalText, Directory, Extension) from AdditionalText
        private static AttributeAndOptions SelectAttributeAndOptions((AttributeContext AttributeContext, GeneratorOptions Options) attributeAndOptions, CancellationToken _) =>
            new(attributeAndOptions.AttributeContext, attributeAndOptions.Options);

        // Selects (AttributeContext, Path, Extension) from (AttributeContext, Options)
        private static AttributeAndGlob SelectAttributesAndGlobs(AttributeAndOptions attributeAndOptions, CancellationToken ct)
        {
            var resourceSearchPath = GetResourceSearchPath(attributeAndOptions.AttributeContext, attributeAndOptions.Options);
            var glob = new Glob(resourceSearchPath, attributeAndOptions.AttributeContext.ExtensionArg);
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
            if (ct.IsCancellationRequested) LightweightTrace.Add((int)TrackingNames.Cancel + 8000);
            ct.ThrowIfCancellationRequested();

            var additionalText = additionalTextAndGlobWithAttributeGlobs.AdditionalTextAndGlob.AdditionalText;
            LightweightTrace.Add((int)TrackingNames.GeneratingDocComment + additionalText.Path.Length * 1000);

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
        private static Dictionary<AttributeContext, EquatableImmutableArray<EmbeddedResource>> GroupResourcesByAttributeContext(
            EquatableImmutableArray<AttributeAndResource> resourceAndAttribute, CancellationToken ct)
        {
            var grouped = new Dictionary<AttributeContext, ImmutableArray<EmbeddedResource>.Builder>();
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
                Dictionary<AttributeContext, EquatableImmutableArray<EmbeddedResource>> ResourcesByAttributeContextLookup
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
            in AttributeContext attributeContext,
            EquatableImmutableArray<EmbeddedResource> embeddedResources,
            in GeneratorOptions options)
        {
            var cancellationToken = context.CancellationToken;
            if (cancellationToken.IsCancellationRequested) LightweightTrace.Add((int)TrackingNames.Cancel + 1000);
            cancellationToken.ThrowIfCancellationRequested();

            var resourceSearchPath = GetResourceSearchPath(attributeContext, options);

            var codeGenerator = new CodeGenerator(
                attributeContext,
                resourceSearchPath,
                embeddedResources,
                options,
                cancellationToken);

            var hintName = attributeContext.DisplayString.GetHintName();
            var source = codeGenerator.GenerateSource();
            context.AddSource(hintName, source);
        }

        private static string GetResourceSearchPath(in AttributeContext attributeContext, in GeneratorOptions options)
        {
            string baseDir;
            string resourceSearchPath;

            var pathArg = attributeContext.PathArg;

            if (pathArg.StartsWith("/") || pathArg.StartsWith("\\"))
            {
                baseDir = options.ProjectDir;
                resourceSearchPath = pathArg.Length > 0 ? pathArg.Substring(1) : string.Empty;
            }
            else
            {
                baseDir = Path.GetDirectoryName(attributeContext.FilePath) ?? string.Empty;
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

    public record struct AttributeAndOptions(AttributeContext AttributeContext, GeneratorOptions Options);
    public record struct Glob(string? Directory, string Extension);
    public record struct AttributeAndGlob(AttributeContext AttributeContext, Glob Glob);
    public record struct AdditionalTextAndGlob(AdditionalText AdditionalText, Glob Glob);
    public record struct AdditionalTextGlobsAndResources(AdditionalTextAndGlobWithAttributeGlobs FileAndGlobs, EmbeddedResource EmbeddedResource);
    public record struct AdditionalTextAndGlobWithAttributeGlobs(AdditionalTextAndGlob AdditionalTextAndGlob, EquatableImmutableArray<Glob> AttributeGlobs);
    public record struct AttributeAndResource(AttributeContext AttributeContext, EmbeddedResource Resource);
    public record struct AttributeOptionsAndResources(AttributeContext Context, GeneratorOptions Options, EquatableImmutableArray<EmbeddedResource> Resources);
}
