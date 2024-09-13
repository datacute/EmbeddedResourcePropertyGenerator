using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public readonly struct AttributeContext
    {
        public readonly string ExtensionArg;
        public readonly string PathArg;

        public readonly string FilePath;

        public readonly bool ContainingNamespaceIsGlobalNamespace;
        public readonly string ContainingNamespaceDisplayString;

        public readonly Accessibility DeclaredAccessibility; // public
        public readonly bool IsStatic;                       // static
        public readonly string RecordStructOrClass;          // (partial) class
        public readonly string Name;                         // ClassName
        public readonly string DisplayString;                // Namespace.ClassName

        public AttributeContext(in GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext)
        {
            var attributeTargetSymbol = (ITypeSymbol)generatorAttributeSyntaxContext.TargetSymbol;

            //todo support multiple attributes
            var attributeData = generatorAttributeSyntaxContext.Attributes[0];
            var args = attributeData.ConstructorArguments;
            ExtensionArg = (args.Length == 0 ? null : args[0].Value as string) ?? ".txt";
            PathArg = (args.Length < 2 ? null : args[1].Value as string) ?? attributeTargetSymbol.Name;
            // override with named arguments
            if (!attributeData.NamedArguments.IsEmpty)
            {
                foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
                {
                    var e = namedArgument.Value.Value?.ToString();
                    if (namedArgument.Key == "Extension" && e != null)
                        ExtensionArg = e;
                    else
                    {
                        var p = namedArgument.Value.Value?.ToString();
                        if (namedArgument.Key == "Path" && p != null) PathArg = p;
                    }
                }
            }

            FilePath = generatorAttributeSyntaxContext.TargetNode.SyntaxTree.FilePath;

            ContainingNamespaceIsGlobalNamespace = attributeTargetSymbol.ContainingNamespace.IsGlobalNamespace;
            ContainingNamespaceDisplayString = attributeTargetSymbol.ContainingNamespace.ToDisplayString();

            DeclaredAccessibility = attributeTargetSymbol.DeclaredAccessibility;
            IsStatic = attributeTargetSymbol.IsStatic;
            RecordStructOrClass = GetRecordStructOrClass(attributeTargetSymbol);
            Name = attributeTargetSymbol.Name;
            DisplayString = attributeTargetSymbol.ToDisplayString();
        }

        private static string GetRecordStructOrClass(ITypeSymbol typeSymbol)
        {
            // This shouldn't be necessary, as only classes are supported
            if (typeSymbol.IsRecord && typeSymbol.IsReferenceType)
                return "record";
            if (typeSymbol.IsRecord)
                return "record struct";
            if (typeSymbol.IsReferenceType)
                return "class";
            return "struct";
        }
    }
}