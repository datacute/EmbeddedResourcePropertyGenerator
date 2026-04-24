using Datacute.IncrementalGeneratorExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public readonly record struct GeneratorOptions(
        bool IsDesignTimeBuild,
        string ProjectDir,
        string RootNamespace,
        bool SupportsFieldKeyword)
    {
        public static GeneratorOptions Select(
            (AnalyzerConfigOptionsProvider AnalyzerConfigOptions, ParseOptions ParseOptions) providers,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested(GeneratorStage.AnalyzerConfigOptionsProviderSelect);
            var options = providers.AnalyzerConfigOptions.GlobalOptions;
            var isDesignTimeBuild =
                options.TryGetValue("build_property.DesignTimeBuild", out var designTimeBuild) &&
                StringComparer.OrdinalIgnoreCase.Equals("true", designTimeBuild);
            var projectDir = options.TryGetValue("build_property.ProjectDir", out var pd) ? pd : string.Empty;
            var rootNamespace = options.TryGetValue("build_property.RootNamespace", out var rn) ? rn : string.Empty;
            // LanguageVersion.CSharp14 = 1400; cast avoids a dependency on a newer Roslyn package
            var supportsFieldKeyword = providers.ParseOptions is CSharpParseOptions csharpOptions
                && (int)csharpOptions.LanguageVersion >= 1400;
            return new GeneratorOptions(isDesignTimeBuild, projectDir, rootNamespace, supportsFieldKeyword);
        }
    }
}
