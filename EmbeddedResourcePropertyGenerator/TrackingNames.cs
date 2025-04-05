namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public class TrackingNames
    {
        public const string OptionGeneration = nameof(OptionGeneration);
        public const string FindAttributes = nameof(FindAttributes);
        public const string AttributesAndOptions = nameof(AttributesAndOptions);
        public const string EmbeddedResourceDocComments = nameof(EmbeddedResourceDocComments);
        public const string Combine = nameof(Combine);
    
        public const int Generator_Initialize = 0;
        public const int AttributeContext_Transform = 1;
        public const int AnalyzerConfigOptionsDescription_Select = 2;
        public const int CompilationDescription_Select = 3;
        public const int ParseOptionsDescription_Select = 4;
        public const int AdditionalTextDescription_Select = 5;
        public const int MetadataReferenceDescription_Select = 6;
        public const int Generator_Action = 7;
        public const int DiagnosticTraceLog_Written = 8;

        public const int DocComment_Select = 9;
        public const int DocComment_Generate = 10;

        public static readonly Dictionary<int, string> TracingNames = new()
        {
            { Generator_Initialize, nameof(Generator_Initialize) },
            { AttributeContext_Transform, nameof(AttributeContext_Transform) },
            { AnalyzerConfigOptionsDescription_Select, nameof(AnalyzerConfigOptionsDescription_Select) },
            { CompilationDescription_Select, nameof(CompilationDescription_Select) },
            { ParseOptionsDescription_Select, nameof(ParseOptionsDescription_Select) },
            { AdditionalTextDescription_Select, nameof(AdditionalTextDescription_Select) },
            { MetadataReferenceDescription_Select, nameof(MetadataReferenceDescription_Select) },
            { Generator_Action, nameof(Generator_Action) },
            { DiagnosticTraceLog_Written, nameof(DiagnosticTraceLog_Written) },
            { DocComment_Select, nameof(DocComment_Select) },
            { DocComment_Generate, nameof(DocComment_Generate) },
        };        
    }
}