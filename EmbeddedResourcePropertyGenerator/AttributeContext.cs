using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public readonly record struct AttributeContext
    {
        public readonly string ExtensionArg;
        public readonly string PathArg;
        public readonly bool TriggerDocCommentCacheRebuildArg = false;
        public readonly bool OutputDiagnosticTraceLog = false;

        public readonly string FilePath;

        public readonly bool ContainingNamespaceIsGlobalNamespace;
        public readonly string ContainingNamespaceDisplayString;

        // Store parent classes with their modifiers
        public record struct ParentClassInfo(
            string Name, 
            bool IsStatic, 
            Accessibility Accessibility,
            string RecordStructOrClass,
            string[] TypeParameters);
        public readonly ParentClassInfo[] ParentClasses { get; }
        public bool HasParentClasses => ParentClasses.Length > 0;

        public readonly Accessibility DeclaredAccessibility; // public
        public readonly bool IsStatic;                       // static
        public readonly string RecordStructOrClass;          // (partial) class
        public readonly string Name;                         // ClassName
        public readonly string[] TypeParameters;               // <T, U>
        public readonly string DisplayString;                // Namespace.ClassName

        public AttributeContext(in GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext)
        {
            LightweightTrace.Add(TrackingNames.AttributeContext_Transform);
            
            var attributeTargetSymbol = (ITypeSymbol)generatorAttributeSyntaxContext.TargetSymbol;

            //todo support multiple attributes
            var attributeData = generatorAttributeSyntaxContext.Attributes[0];
            var args = attributeData.ConstructorArguments;
            ExtensionArg = (args.Length == 0 ? null : args[0].Value as string) ?? ".txt";
            PathArg = (args.Length < 2 ? null : args[1].Value as string) ?? attributeTargetSymbol.Name;
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
                                    case "RegenerateDocCommentsWhileEditing":
                                        TriggerDocCommentCacheRebuildArg = boolValue;
                                        break;
                                    case "DiagnosticTraceLog":
                                        OutputDiagnosticTraceLog = boolValue;
                                        break;
                                }

                                break;
                            case string stringValue:
                                switch (namedArgument.Key)
                                {
                                    case "Extension":
                                        ExtensionArg = stringValue;
                                        break;
                                    case "Path":
                                        PathArg = stringValue;
                                        break;
                                }

                                break;
                        }
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

            if (generatorAttributeSyntaxContext.TargetSymbol is INamedTypeSymbol namedTypeTargetSymbol)
            {
                TypeParameters = namedTypeTargetSymbol.TypeParameters.Select(tp => tp.Name).ToArray();
            }
            else
            {
                TypeParameters = Array.Empty<string>();
            }

            DisplayString = attributeTargetSymbol.ToDisplayString();
        
            // Parse parent classes from symbol's containing types
            var parentClasses = new List<ParentClassInfo>();
            var containingType = attributeTargetSymbol.ContainingType;
            while (containingType != null)
            {
                var typeParams = containingType.TypeParameters.Select(tp => tp.Name).ToArray();
            
                parentClasses.Insert(0, new ParentClassInfo(
                    containingType.Name, 
                    containingType.IsStatic,
                    containingType.DeclaredAccessibility,
                    GetRecordStructOrClass(containingType),
                    typeParams));
                containingType = containingType.ContainingType;
            }

            ParentClasses = parentClasses.ToArray();
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