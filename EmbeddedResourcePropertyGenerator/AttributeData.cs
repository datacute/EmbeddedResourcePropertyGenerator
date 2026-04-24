using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator;

public readonly record struct AttributeData(
    string ExtensionArg,
    string PathArg,
    bool OutputDiagnosticTraceLog,
    string FilePath)
{
    public static AttributeData Collector(GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext)
    {
        var attributeData = generatorAttributeSyntaxContext.Attributes[0];
        var args = attributeData.ConstructorArguments;
        var extensionArg = (args.Length == 0 ? null : args[0].Value as string) ?? ".txt";
        var pathArg = (args.Length < 2 ? null : args[1].Value as string) ?? generatorAttributeSyntaxContext.TargetSymbol.Name;
        var outputDiagnosticTraceLog = false;
        if (!attributeData.NamedArguments.IsEmpty)
        {
            foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
            {
                var value = namedArgument.Value.Value;
                if (value != null)
                {
                    switch (value)
                    {
                        case bool boolValue:
                            switch (namedArgument.Key)
                            {
                                case "DiagnosticTraceLog":
                                    outputDiagnosticTraceLog = boolValue;
                                    break;
                            }
                            break;
                        case string stringValue:
                            switch (namedArgument.Key)
                            {
                                case "Extension":
                                    extensionArg = stringValue;
                                    break;
                                case "Path":
                                    pathArg = stringValue;
                                    break;
                            }
                            break;
                    }
                }
            }
        }
        var filePath = generatorAttributeSyntaxContext.TargetNode.SyntaxTree.FilePath;
        return new AttributeData(extensionArg, pathArg, outputDiagnosticTraceLog, filePath);
    }
}