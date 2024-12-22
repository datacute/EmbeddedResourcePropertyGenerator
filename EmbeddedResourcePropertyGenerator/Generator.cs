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
                .Select(GeneratorOptions.Select);

            var attributeContexts = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    Templates.AttributeFullyQualified,
                    predicate: (node, _) => node is TypeDeclarationSyntax,
                    transform: (attributeSyntaxContext, _) => new AttributeContext(attributeSyntaxContext))
                .WithTrackingName(TrackingNames.InitialExtraction);

            var attributesContextsAndMatchingEmbeddedResources = 
                attributeContexts.Combine(context.AdditionalTextsProvider.Collect()).Combine(options)
                    .Select(MatchAdditionalFiles)
                    .WithTrackingName(TrackingNames.MatchAdditionalFiles);

            var attributesWithFilesAndOptions = 
                attributesContextsAndMatchingEmbeddedResources.Combine(options)
                    .WithTrackingName(TrackingNames.Combine);

            context.RegisterSourceOutput(attributesWithFilesAndOptions,
                (sourceProductionContext, attributeWithFilesAndOptions) =>
                {
                    var ((attributeContext, additionalTexts), generatorOptions) = attributeWithFilesAndOptions;
                    GenerateFolderEmbed(sourceProductionContext, attributeContext, additionalTexts, generatorOptions);
                });
        }

        private (AttributeContext AttributeContext, ImmutableArray<EmbeddedResource> AdditionalTexts)
            MatchAdditionalFiles(
                ((AttributeContext AttributeContext, ImmutableArray<AdditionalText> AdditionalTexts) AttributeContextAndAdditionalTexts,
                    GeneratorOptions Options) attributeContextTextsAndOptions,
                CancellationToken ct)
        {
            var attributeContext = attributeContextTextsAndOptions.AttributeContextAndAdditionalTexts.AttributeContext;
            var additionalTexts = attributeContextTextsAndOptions.AttributeContextAndAdditionalTexts.AdditionalTexts;
            var options = attributeContextTextsAndOptions.Options;

            var resourceSearchPath = GetResourceSearchPath(attributeContext, options);

            var matchingAdditionalTexts = additionalTexts
                .Where(t => FileIsInMatchingFolder(t.Path, resourceSearchPath, attributeContext.ExtensionArg))
                .Select(additionalText =>
                {
                    var docCommentCode = AdditionalTextDocCommentCreator.GenerateDocCommentCode(additionalText, ct);
                    return new EmbeddedResource(additionalText.Path, docCommentCode);
                })
                .ToImmutableArray();

            return (attributeContext, matchingAdditionalTexts);
        }

        private bool FileIsInMatchingFolder(string resourceFilePath, string resourceSearchPath, string extensionArg) =>
            Path.GetDirectoryName(resourceFilePath) == resourceSearchPath
            && Path.GetExtension(resourceFilePath) == extensionArg;

        private static void GenerateFolderEmbed(
            in SourceProductionContext context,
            in AttributeContext attributeContext,
            ImmutableArray<EmbeddedResource> additionalTexts,
            in GeneratorOptions options)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            var resourceSearchPath = GetResourceSearchPath(attributeContext, options);

            var codeGenerator = new CodeGenerator(
                attributeContext,
                resourceSearchPath,
                additionalTexts,
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
