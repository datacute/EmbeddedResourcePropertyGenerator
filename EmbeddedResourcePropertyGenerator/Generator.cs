using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var options = context.AnalyzerConfigOptionsProvider
                .Select(GeneratorOptions.Select)
                .WithTrackingName(TrackingNames.OptionGeneration);

            var attributeContexts = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    Templates.AttributeFullyQualified,
                    predicate: (node, _) => node is TypeDeclarationSyntax,
                    transform: (attributeSyntaxContext, _) => new AttributeContext(attributeSyntaxContext))
                .WithTrackingName(TrackingNames.FindAttributes);

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
            var attributeContext = attributeContextOptionsAndAdditionalTexts.AttributeContextAndOptions.AttributeContext;
            var options = attributeContextOptionsAndAdditionalTexts.AttributeContextAndOptions.Options;
            var additionalTexts = attributeContextOptionsAndAdditionalTexts.AdditionalTexts;

            var resourceSearchPath = GetResourceSearchPath(attributeContext, options);

            var embeddedResources = additionalTexts
                .Where(t => FileIsInMatchingFolder(t.Path, resourceSearchPath, attributeContext.ExtensionArg))
                .Select(additionalText =>
                {
                    // Skip generating doc comments during design-time builds
                    var docCommentCode = options.IsDesignTimeBuild ? null : AdditionalTextDocCommentCreator.GenerateDocCommentCode(additionalText, ct);
                    return new EmbeddedResource(additionalText.Path, docCommentCode!);
                }).ToImmutableEquatableArray();

            return (attributeContext, embeddedResources);
        }

        private bool FileIsInMatchingFolder(string resourceFilePath, string resourceSearchPath, string extensionArg) =>
            Path.GetDirectoryName(resourceFilePath) == resourceSearchPath
            && Path.GetExtension(resourceFilePath) == extensionArg;

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
