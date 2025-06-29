using System.Collections.Generic; // Required for Dictionary

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    public enum TrackingNames
    {
        GeneratorInitialized = 0,
        Cancel = 1,

        // Pipeline Stages
        AttributeContextsCreated = 10,          // Step 1: AttributeContexts created
        AnalyzerConfigOptions = 20,             // Step 2: Options selected
        AttributesAndOptionsCombined = 30,      // Step 3: Attributes and Options combined
        FileInfoSelected = 40,                  // Step 4a: AdditionalText -> FileInfo (Path, Dir, Ext)
        AttributeGlobInfoSelected = 41,         // Helper: Path/Extension selected for attributes
        AttributeGlobsSelected = 42,             // Helper: Just Path/Extension for resource matching
        FileInfoAndGlobsCombined = 43,          // Step 4b: FileInfo combined with ResourceGlobs
        MatchingFilesFiltered = 44,             // Step 4c: Files filtered by glob match
        EmbeddedResourceExtracted = 46,         // Step 4d: EmbeddedResource created (with FileAndGlobs info)
        ResourceAndAllAttributeGlobsCombined = 47, // Step 4e: Resource/File data combined with all AttributeGlobInfo
        MatchingResourceAndAttributeSelected = 48, // Step 4f: (AttributeContext, EmbeddedResource) selected
        ResourcesGroupedByAttributeContext = 50, // Step 5: Resources grouped into lookup
        GenerationInputPrepared = 60,           // Step 6: Final data prepared for output (AttributeContext, Resources, Options)

        // Execution/Action Steps
        GeneratingDocComment = 45,              // Action: Doc comment generation started
        GeneratingSourceFile = 70,              // Step 7: Source generation started
        DiagnosticTraceLogWritten = 80          // Action: Trace log output written (if enabled)
    }

    public static class TrackingNameDescriptions // Made static as it only contains static members
    {
        // Updated dictionary with new names and descriptions
        public static readonly Dictionary<int, string> EventNameMap = new()
        {
            { (int)TrackingNames.GeneratorInitialized, "Generator Initialized" },
            { (int)TrackingNames.Cancel, "Operation Cancelled" },

            // Pipeline Stages
            { (int)TrackingNames.AttributeContextsCreated, "Created AttributeContexts" },
            { (int)TrackingNames.AnalyzerConfigOptions, "Selected GeneratorOptions" },
            { (int)TrackingNames.AttributesAndOptionsCombined, "Combined Attributes and Options" },
            { (int)TrackingNames.FileInfoSelected, "Selected File Info (Path/Dir/Ext) from AdditionalText" },
            { (int)TrackingNames.AttributeGlobInfoSelected, "Selected Attribute Glob Info (Path/Ext)" },
            { (int)TrackingNames.AttributeGlobsSelected, "Selected Attribute Globs (Path/Ext)" },
            { (int)TrackingNames.FileInfoAndGlobsCombined, "Combined File Info and Resource Globs" },
            { (int)TrackingNames.MatchingFilesFiltered, "Filtered Files Matching Globs" },
            { (int)TrackingNames.EmbeddedResourceExtracted, "Extracted EmbeddedResource (with File/Glob info)" },
            { (int)TrackingNames.ResourceAndAllAttributeGlobsCombined, "Combined Resource/File Data and All Attribute Glob Info" },
            { (int)TrackingNames.MatchingResourceAndAttributeSelected, "Selected Matching (AttributeContext, EmbeddedResource)" },
            { (int)TrackingNames.ResourcesGroupedByAttributeContext, "Grouped Resources by AttributeContext into Lookup" },
            { (int)TrackingNames.GenerationInputPrepared, "Prepared Final Generation Input (AttrContext, Resources, Options)" },

            // Execution/Action Steps
            { (int)TrackingNames.GeneratingDocComment, "Generating Doc Comment" },
            { (int)TrackingNames.GeneratingSourceFile, "Generating Source File" },
            { (int)TrackingNames.DiagnosticTraceLogWritten, "Diagnostic Trace Log Written" },
        };
    }
}