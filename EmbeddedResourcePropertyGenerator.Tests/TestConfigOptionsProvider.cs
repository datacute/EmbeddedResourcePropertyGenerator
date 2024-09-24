using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace EmbeddedResourcePropertyGenerator.Tests;

public class TestConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotImplementedException();

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new NotImplementedException();

    public override AnalyzerConfigOptions GlobalOptions { get; } = new TestConfigOptions();
}

public class TestConfigOptions : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        switch (key)
        {
            case "build_property.DesignTimeBuild":
                value = "false";
                return true;
            case "build_property.ProjectDir":
                value = TestHelper.TestPath("/");
                return true;
            case "build_property.RootNamespace":
                value = TestHelper.TestNamespace;
                return true;
            default:
                value = null;
                return false;
        }
    }
}