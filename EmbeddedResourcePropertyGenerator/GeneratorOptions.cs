using Microsoft.CodeAnalysis.Diagnostics;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public readonly struct GeneratorOptions
    {
        public readonly bool IsDesignTimeBuild;
        public readonly string ProjectDir;
        public readonly string RootNamespace;

        private GeneratorOptions(AnalyzerConfigOptions options)
        {
            IsDesignTimeBuild =
                options.TryGetValue("build_property.DesignTimeBuild", out var designTimeBuild) &&
                StringComparer.OrdinalIgnoreCase.Equals("true", designTimeBuild);
            ProjectDir = options.TryGetValue("build_property.ProjectDir", out var projectDir) ? projectDir : string.Empty;
            RootNamespace = options.TryGetValue("build_property.RootNamespace", out var rootNamespace) ? rootNamespace : string.Empty;
        }

        public static GeneratorOptions Select(AnalyzerConfigOptionsProvider provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new GeneratorOptions(provider.GlobalOptions);
        }
    }
}
