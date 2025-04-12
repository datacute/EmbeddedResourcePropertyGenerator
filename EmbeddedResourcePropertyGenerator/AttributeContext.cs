using System.Collections.Immutable;
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
            EquatableImmutableArray<string> TypeParameters);

        public readonly EquatableImmutableArray<ParentClassInfo> ParentClasses;
        public bool HasParentClasses => ParentClasses.Count > 0;

        public readonly Accessibility DeclaredAccessibility; // public
        public readonly bool IsStatic;                       // static
        public readonly string RecordStructOrClass;          // (partial) class
        public readonly string Name;                         // ClassName
        public readonly EquatableImmutableArray<string> TypeParameters;               // <T, U>
        public readonly string DisplayString;                // Namespace.ClassName

        public AttributeContext(in GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext)
        {
            // No diagnostic tracing here - this triggers for each matching attribute, every time you type.
            // the time taken within this method is about 1% ot the time the source generator takes
            // to process all the attributes.

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
                var typeParameters = namedTypeTargetSymbol.TypeParameters;
                TypeParameters = typeParameters.Length > 0 ?
                    typeParameters.ToEquatableImmutableArray(tp => tp.Name) :
                    EquatableImmutableArray<string>.Empty;
            }
            else
            {
                TypeParameters = EquatableImmutableArray<string>.Empty;
            }

            DisplayString = attributeTargetSymbol.ToDisplayString();
        
            // Parse parent classes from symbol's containing types
            var parentClassCount = 0;
            var containingType = attributeTargetSymbol.ContainingType;
            // Count the number of parent classes
            while (containingType != null)
            {
                parentClassCount++;
                containingType = containingType.ContainingType;
            }

            if (parentClassCount > 0)
            {
                containingType = attributeTargetSymbol.ContainingType;
                var parentClassImmutableArrayBuilder = ImmutableArray.CreateBuilder<ParentClassInfo>(parentClassCount);
                for (var i = 0; i < parentClassCount; i++)
                {
                    var typeParameters = containingType.TypeParameters;
                    var typeParameterNames = typeParameters.Length > 0 ? 
                        typeParameters.ToEquatableImmutableArray(tp => tp.Name) : 
                        EquatableImmutableArray<string>.Empty;

                    parentClassImmutableArrayBuilder.Insert(0, new ParentClassInfo(
                        containingType.Name, 
                        containingType.IsStatic,
                        containingType.DeclaredAccessibility,
                        GetRecordStructOrClass(containingType),
                        typeParameterNames));
                    containingType = containingType.ContainingType;
                }

                ParentClasses = parentClassImmutableArrayBuilder.MoveToImmutable().ToEquatableImmutableArray();
            }
            else
            {
                ParentClasses = EquatableImmutableArray<ParentClassInfo>.Empty;
            }
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