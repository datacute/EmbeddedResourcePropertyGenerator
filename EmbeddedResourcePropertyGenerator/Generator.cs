using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            LightweightTrace.Add((int)TrackingNames.GeneratorInitialized);
            
            var attributes = 
                context.SyntaxProvider
                    .ForAttributeWithMetadataName(
                        Templates.AttributeFullyQualified,
                        predicate: (node, _) => node is TypeDeclarationSyntax,
                        transform: (attributeSyntaxContext, _) => new AttributeContext(attributeSyntaxContext))
                    .Trace(TrackingNames.AttributeChange);

            var options = 
                context.AnalyzerConfigOptionsProvider
                    .Select(GeneratorOptions.Select)
                    .Trace(TrackingNames.AnalyzerConfigOptions);

            var attributesAndOptions = 
                attributes
                    .Combine(options)
                    .Trace(TrackingNames.CombineAttributesAndOptions);

            var attributesAndGlobs = 
                attributesAndOptions
                    .Select(SelectAttributesAndGlobs)
                    .Trace(TrackingNames.SelectAttributesAndGlobs);

            var globs = 
                attributesAndGlobs
                    .Select(JustTheGlobs)
                    .Trace(TrackingNames.JustTheGlobs);

            var additionalTextsEmbeddedResourcesAndMatchingAttributes = 
                context.AdditionalTextsProvider
                    .Select(AddDirectoryAndExtension)
                    .Trace(TrackingNames.AdditionalTextAndPaths)
                    .CombineEquatable(globs)
                    .Trace(TrackingNames.AdditionalTextPathsAndGlobs)
                    .Where(AdditionalTextMatches)
                    .Trace(TrackingNames.AdditionalTextMatches)
                    .Select(ExtractMatchingEmbeddedResourceDocComments)
                    .Trace(TrackingNames.AdditionalTextGlobsAndEmbeddedResource)
                    .CombineEquatable(attributesAndGlobs)
                    .Trace(TrackingNames.AdditionalTextPathsAndGlobsEmbeddedResourcesAndAttributes)
                    .Select(ReduceToMatchingContexts)
                    .Trace(TrackingNames.AdditionalTextEmbeddedResourcesAndMatchingAttributes);

            var attributesEmbeddedResourcesAndOptions =
                options.CombineEquatable(additionalTextsEmbeddedResourcesAndMatchingAttributes).Trace(TrackingNames.CombineOptionsWithAdditionalTextsEmbeddedResourceAndMatchingAttributes).WithTrackingName(nameof(TrackingNames.CombineOptionsWithAdditionalTextsEmbeddedResourceAndMatchingAttributes))
                    .CombineEquatable(attributes)
                    .Trace(TrackingNames.CombineOptionsAdditionalTextsEmbeddedResourceAndMatchingAttributesWithAttributes)
                    .SelectMany(GroupByAttribute)
                    .Trace(TrackingNames.AttributeEmbeddedResourceAndOptions);
            
            context.RegisterSourceOutput(attributesEmbeddedResourcesAndOptions,
                (sourceProductionContext, attributeEmbeddedResourcesAndOptions) =>
                {
                    LightweightTrace.Add((int)TrackingNames.GeneratingSourceFile);

                    var (attributeContext, embeddedResources, generatorOptions) = attributeEmbeddedResourcesAndOptions;
                    GenerateFolderEmbed(sourceProductionContext, attributeContext, embeddedResources, generatorOptions);
                });
        }

        private (string Path, string Extension) JustTheGlobs(
            (AttributeContext AttributeContext, string Path, string Extension) globsAndContext, 
            CancellationToken ct) => (globsAndContext.Path, globsAndContext.Extension);

        private (AdditionalText AdditionalText, string Directory, string Extension) AddDirectoryAndExtension(AdditionalText additionalText, 
            CancellationToken ct) => (additionalText, Path.GetDirectoryName(additionalText.Path), Path.GetExtension(additionalText.Path));

        private (AdditionalText AdditionalText, EmbeddedResource EmbeddedResource, EquatableImmutableArray<AttributeContext> MatchingContexts) 
            ReduceToMatchingContexts(
                (
                    (
                        (
                            (AdditionalText AdditionalText, string Directory, string Extension) AdditionalTextAndPaths, 
                            EquatableImmutableArray<(string Path, string Extension)> Globs
                        ) AdditionalTextsAndGlobs, 
                        EmbeddedResource EmbeddedResource
                    ) ResourceAndGlobs, 
                    EquatableImmutableArray<(AttributeContext AttributeContext, string Path, string Extension)> AttributesAndGlobs
                ) inputs,
                CancellationToken ct)
        {
            LightweightTrace.Add((int)TrackingNames.ReduceToMatchingContexts);

            var ((((additionalText, directory, extension), _), 
                embeddedResource), attributesAndGlobs) = inputs;

            var matchingContexts = attributesAndGlobs
                .Where(attributeAndGlob => directory == attributeAndGlob.Path && extension == attributeAndGlob.Extension)
                .Select(attributeAndGlob => attributeAndGlob.AttributeContext).ToEquatableImmutableArray();

            return (additionalText, embeddedResource, matchingContexts);
        }

        private (AttributeContext AttributeContext, string Path, string Extension) 
            SelectAttributesAndGlobs(
                (AttributeContext AttributeContext, GeneratorOptions Options) attributeContextAndOptions,
                CancellationToken ct)
        {
            var attributeContext = attributeContextAndOptions.AttributeContext;
            var options = attributeContextAndOptions.Options;
            var path = GetResourceSearchPath(attributeContext, options);
            return (attributeContext, path, attributeContext.ExtensionArg);
        }

        private bool AdditionalTextMatches(
            (
                (AdditionalText AdditionalText, string Directory, string Extension) AdditionalTextAndPaths, 
                EquatableImmutableArray<(string Path, string Extension)> Globs
                ) additionalTextsAndGlobs)
        {
            var (_, directory, extension) = additionalTextsAndGlobs.AdditionalTextAndPaths;

            return additionalTextsAndGlobs.Globs.Any(glob =>
                directory == glob.Path && extension == glob.Extension);
        }

        private (((AdditionalText AdditionalText, string Directory, string Extension) AdditionalTextAndPaths, EquatableImmutableArray<(string Path, string Extension)> Globs) AdditionalTextsAndGlobs, EmbeddedResource EmbeddedResource)
            ExtractMatchingEmbeddedResourceDocComments(
                (
                    (AdditionalText AdditionalText, string Directory, string Extension) AdditionalTextAndPaths, 
                    EquatableImmutableArray<(string Path, string Extension)> Globs
                ) additionalTextsAndGlobs,
                CancellationToken ct)
        {
            if (ct.IsCancellationRequested) LightweightTrace.Add((int)TrackingNames.Cancel + 8000);
            ct.ThrowIfCancellationRequested();

            var additionalText = additionalTextsAndGlobs.AdditionalTextAndPaths.AdditionalText;

            LightweightTrace.Add((int)TrackingNames.GeneratingDocComment + additionalText.Path.Length * 1000);
            
            var docCommentCode = AdditionalTextDocCommentCreator.GenerateDocCommentCode(additionalText, ct);
            var embeddedResource = new EmbeddedResource(additionalText.Path, docCommentCode);

            return (additionalTextsAndGlobs, embeddedResource);
        }

        private EquatableImmutableArray<(AttributeContext Context, EquatableImmutableArray<EmbeddedResource> EmbeddedResources, GeneratorOptions Options)> 
            GroupByAttribute(
                (
                    (
                        GeneratorOptions Options, 
                        EquatableImmutableArray<(
                            AdditionalText AdditionalText, 
                            EmbeddedResource EmbeddedResource, 
                            EquatableImmutableArray<AttributeContext> MatchingContexts
                            )> Resources
                    ) OptionsAndResources, 
                    EquatableImmutableArray<AttributeContext> AttributeContexts) optionsResourcesAndAttributeContexts,
                CancellationToken ct)
        {
            LightweightTrace.Add((int)TrackingNames.GroupByAttribute);

            var optionsAndResources = optionsResourcesAndAttributeContexts.OptionsAndResources;
            var attributeContexts = optionsResourcesAndAttributeContexts.AttributeContexts;
            var options = optionsAndResources.Options;
            var resources = optionsAndResources.Resources;

            var contextsEmbeddedResourcesAndOptions = attributeContexts
                .ToEquatableImmutableArray(context => 
                    (
                        context, 
                        resources.Where(resource => resource.MatchingContexts.Contains(context))
                            .Select(additionalTextAndContexts => additionalTextAndContexts.EmbeddedResource)
                            .ToEquatableImmutableArray(),
                        options
                    )
                );

            return contextsEmbeddedResourcesAndOptions;
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

            if (attributeContext.PathArg.StartsWith("/") || attributeContext.PathArg.StartsWith("\\"))
            {
                baseDir = options.ProjectDir;
                resourceSearchPath = attributeContext.PathArg.Substring(1);
            }
            else
            {
                baseDir = Path.GetDirectoryName(attributeContext.FilePath) ?? string.Empty;
                resourceSearchPath = attributeContext.PathArg;
            }

            return Path.GetFullPath(Path.Combine(baseDir, resourceSearchPath));
        }
    }
}
