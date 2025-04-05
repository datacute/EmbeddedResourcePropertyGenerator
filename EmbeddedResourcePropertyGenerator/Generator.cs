using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class Generator : IIncrementalGenerator
    {
        private readonly Dictionary<string, EmbeddedResource> _embeddedResourceCache = new();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            LightweightTrace.Add(TrackingNames.Generator_Initialize);
            
            var attributeContexts = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    Templates.AttributeFullyQualified,
                    predicate: (node, _) => node is TypeDeclarationSyntax,
                    transform: (attributeSyntaxContext, _) => new AttributeContext(attributeSyntaxContext))
                .WithTrackingName(TrackingNames.FindAttributes);

            var options = context.AnalyzerConfigOptionsProvider
                .Select(GeneratorOptions.Select)
                .WithTrackingName(TrackingNames.OptionGeneration);

            var attributeContextsAndOptions = attributeContexts.Combine(options)
                .WithTrackingName(TrackingNames.AttributesAndOptions);

            var attributesContextsAndMatchingEmbeddedResources = 
                attributeContextsAndOptions.Combine(context.AdditionalTextsProvider.Collect())
                    .Select(ExtractEmbeddedResourceDocComments)
                    .WithTrackingName(TrackingNames.EmbeddedResourceDocComments);

            var attributesWithFilesAndOptions = 
                attributesContextsAndMatchingEmbeddedResources.Combine(options)
                    .WithTrackingName(TrackingNames.Combine);

            context.RegisterSourceOutput(attributesWithFilesAndOptions,
                (sourceProductionContext, attributeWithFilesAndOptions) =>
                {
                    LightweightTrace.Add(TrackingNames.Generator_Action);

                    var ((attributeContext, embeddedResources), generatorOptions) = attributeWithFilesAndOptions;
                    GenerateFolderEmbed(sourceProductionContext, attributeContext, embeddedResources, generatorOptions);
                });
        }

        private (AttributeContext AttributeContext, ImmutableEquatableArray<EmbeddedResource> EmbeddedResources)
            ExtractEmbeddedResourceDocComments(
                ((AttributeContext AttributeContext, GeneratorOptions Options) AttributeContextAndOptions, 
                    ImmutableArray<AdditionalText> AdditionalTexts) attributeContextOptionsAndAdditionalTexts,
                CancellationToken ct)
        {
            LightweightTrace.Add(TrackingNames.DocComment_Select);

            var attributeContext = attributeContextOptionsAndAdditionalTexts.AttributeContextAndOptions.AttributeContext;
            var options = attributeContextOptionsAndAdditionalTexts.AttributeContextAndOptions.Options;
            var additionalTexts = attributeContextOptionsAndAdditionalTexts.AdditionalTexts;

            var resourceSearchPath = GetResourceSearchPath(attributeContext, options);

            var embeddedResources = additionalTexts
                .Where(additionalText => FileIsInMatchingFolder(additionalText, resourceSearchPath, attributeContext))
                .Select(additionalText => GetDocCommentCode(ct, additionalText, options, attributeContext))
                .ToImmutableEquatableArray();

            return (attributeContext, embeddedResources);
        }

        private static bool FileIsInMatchingFolder(
            AdditionalText additionalText,
            string resourceSearchPath,
            AttributeContext attributeContext)
        {
            return Path.GetDirectoryName(additionalText.Path) == resourceSearchPath &&
                   Path.GetExtension(additionalText.Path) == attributeContext.ExtensionArg;
        }

        private EmbeddedResource GetDocCommentCode(
            CancellationToken ct, 
            AdditionalText additionalText,
            GeneratorOptions options, 
            AttributeContext attributeContext)
        {
            // Skip generating doc comments during design-time builds
            if (options.IsDesignTimeBuild)
            {
                return new EmbeddedResource(additionalText.Path, null);
            }

            if (attributeContext.TriggerDocCommentCacheRebuildArg || !_embeddedResourceCache.TryGetValue(additionalText.Path, out var embeddedResource))
            {
                LightweightTrace.Add(TrackingNames.DocComment_Generate);

                // This is the first time we've seen this file, so read the file and generate the doc comments
                var docCommentCode = AdditionalTextDocCommentCreator.GenerateDocCommentCode(additionalText, ct);
                embeddedResource = new EmbeddedResource(additionalText.Path, docCommentCode);
                if (docCommentCode is not null)
                {
                    _embeddedResourceCache[additionalText.Path] = embeddedResource;
                }
            }

            return embeddedResource;
        }

        private static void GenerateFolderEmbed(
            in SourceProductionContext context,
            in AttributeContext attributeContext,
            ImmutableEquatableArray<EmbeddedResource> embeddedResources,
            in GeneratorOptions options)
        {
            var cancellationToken = context.CancellationToken;
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
