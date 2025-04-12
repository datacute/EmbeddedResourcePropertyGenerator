namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public enum TrackingNames
    {
        GeneratorInitialized = 0,
        Cancel = 1,

        AnalyzerConfigOptions = 10,
        AdditionalTextAndPaths = 20,
        AttributeChange = 31,
        CombineAttributesAndOptions = 32,
        SelectAttributesAndGlobs = 33,
        JustTheGlobs = 34,
        AdditionalTextPathsAndGlobs = 41,
        AdditionalTextMatches = 42,
        GeneratingDocComment = 43,
        AdditionalTextGlobsAndEmbeddedResource = 44,
        AdditionalTextPathsAndGlobsEmbeddedResourcesAndAttributes = 45,
        ReduceToMatchingContexts = 46,
        AdditionalTextEmbeddedResourcesAndMatchingAttributes = 47,
        CombineOptionsWithAdditionalTextsEmbeddedResourceAndMatchingAttributes = 50,
        CombineOptionsAdditionalTextsEmbeddedResourceAndMatchingAttributesWithAttributes = 61,
        GroupByAttribute = 62,
        AttributeEmbeddedResourceAndOptions = 70,
        GeneratingSourceFile = 81,
        DiagnosticTraceLogWritten = 82
    }
    
    public class TrackingNameDescriptions
    {
        public static readonly Dictionary<int, string> EventNameMap = new()
        {
            { (int)TrackingNames.GeneratorInitialized, " Generator Initialized" },
            { (int)TrackingNames.AnalyzerConfigOptions, "new AnalyzerConfigOptions" },
            { (int)TrackingNames.AdditionalTextAndPaths, "new Additional Text and Paths (Directory and Extension)" },
            { (int)TrackingNames.AttributeChange, "new Attribute" },
            { (int)TrackingNames.CombineAttributesAndOptions, "new Combination of Attributes and Options" },
            { (int)TrackingNames.SelectAttributesAndGlobs, "new Selection of Attributes and Globs" },
            { (int)TrackingNames.JustTheGlobs, "new Glob" },
            { (int)TrackingNames.AdditionalTextPathsAndGlobs, "new Selection of Additional Texts, Paths, and Globs" },
            { (int)TrackingNames.AdditionalTextMatches, "new AdditionalText Matches to Globs" },
            { (int)TrackingNames.GeneratingDocComment, "** Generating doc-comment for Additional Texts **" },
            { (int)TrackingNames.AdditionalTextGlobsAndEmbeddedResource, "new AdditionalText, Globs, and EmbeddedResource" },
            { (int)TrackingNames.AdditionalTextPathsAndGlobsEmbeddedResourcesAndAttributes, "new AdditionalText, Paths and Globs, EmbeddedResources, and Attributes" },
            { (int)TrackingNames.ReduceToMatchingContexts, "Reduce to matching Contexts" },
            { (int)TrackingNames.AdditionalTextEmbeddedResourcesAndMatchingAttributes, "new AdditionalText, EmbeddedResources, and matching Attributes" },
            { (int)TrackingNames.CombineOptionsWithAdditionalTextsEmbeddedResourceAndMatchingAttributes, "new Combination of Options with AdditionalTexts, EmbeddedResource, and matching Attributes" },
            { (int)TrackingNames.CombineOptionsAdditionalTextsEmbeddedResourceAndMatchingAttributesWithAttributes, "new Combination of Options with AdditionalTexts, EmbeddedResource, and matching Attributes with all Attributes" },
            { (int)TrackingNames.GroupByAttribute, "Group By Attribute" },
            { (int)TrackingNames.AttributeEmbeddedResourceAndOptions, "new Attribute, EmbeddedResource, and Options" },
            { (int)TrackingNames.GeneratingSourceFile, "Generating Source File" },
            { (int)TrackingNames.DiagnosticTraceLogWritten, "Diagnostic Trace Log Written" },
            { (int)TrackingNames.Cancel, "Cancel" },
        };
    }
}