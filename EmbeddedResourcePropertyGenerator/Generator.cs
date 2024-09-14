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
            context.RegisterPostInitializationOutput(GenerateInitialCode);

            var options = context.AnalyzerConfigOptionsProvider
                .Select(GeneratorOptions.Select);

            var attributeContexts = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    Templates.AttributeFullyQualified,
                    predicate: (node, _) => node is TypeDeclarationSyntax,
                    transform: (attributeSyntaxContext, _) => new AttributeContext(attributeSyntaxContext))
                .WithTrackingName(TrackingNames.InitialExtraction);

            var attributesWithFilesAndOptions = attributeContexts
                .Combine(context.AdditionalTextsProvider.Collect().Combine(options))
                .WithTrackingName(TrackingNames.Combine);

            context.RegisterSourceOutput(attributesWithFilesAndOptions,
                (sourceProductionContext, attributeWithFilesAndOptions) =>
                {
                    var (attributeContext, (additionalTexts, generatorOptions)) = attributeWithFilesAndOptions;
                    GenerateFolderEmbed(sourceProductionContext, attributeContext, additionalTexts, generatorOptions);
                });
        }

        private static void GenerateInitialCode(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("_EmbeddedResourceReader.g.cs", Templates.EmbeddedResourceReaderCs);
        }

        private static void GenerateFolderEmbed(
            in SourceProductionContext context,
            in AttributeContext attributeContext,
            ImmutableArray<AdditionalText> additionalTexts,
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

            if (attributeContext.PathArg.StartsWith("/"))
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
