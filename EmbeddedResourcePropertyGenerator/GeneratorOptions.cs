using Datacute.IncrementalGeneratorExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public readonly record struct GeneratorOptions
    {
        public readonly bool IsDesignTimeBuild;
        public readonly string ProjectDir;
        public readonly string RootNamespace;
        public readonly bool SupportsFieldKeyword;

        private GeneratorOptions(AnalyzerConfigOptions options, bool supportsFieldKeyword)
        {
            IsDesignTimeBuild =
                options.TryGetValue("build_property.DesignTimeBuild", out var designTimeBuild) &&
                StringComparer.OrdinalIgnoreCase.Equals("true", designTimeBuild);
            ProjectDir = options.TryGetValue("build_property.ProjectDir", out var projectDir) ? projectDir : string.Empty;
            RootNamespace = options.TryGetValue("build_property.RootNamespace", out var rootNamespace) ? rootNamespace : string.Empty;
            SupportsFieldKeyword = supportsFieldKeyword;
        }

        public static GeneratorOptions Select(
            (AnalyzerConfigOptionsProvider AnalyzerConfigOptions, ParseOptions ParseOptions) providers,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested(GeneratorStage.AnalyzerConfigOptionsProviderSelect);
            // LanguageVersion.CSharp14 = 1400; cast avoids a dependency on a newer Roslyn package
            var supportsFieldKeyword = providers.ParseOptions is CSharpParseOptions csharpOptions
                && (int)csharpOptions.LanguageVersion >= 1400;
            return new GeneratorOptions(providers.AnalyzerConfigOptions.GlobalOptions, supportsFieldKeyword);
        }
    }
}
