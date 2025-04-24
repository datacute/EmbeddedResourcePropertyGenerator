# Mermaid Diagram of Generator Pipelines

```mermaid
---
title: Generator Pipelines
config:
  theme: neutral
  look: neo
---
flowchart TD
    subgraph subGraph0["Input Providers"]
        direction LR
        SP["SyntaxProvider"]
        SC["Source Code"]
        ACOP["AnalyzerConfigOptionsProvider"]
        CF["Config"]
        ATP["AdditionalTextsProvider"]
        AT["Additional Text"]
    end
    subgraph subGraph1["Pipeline Stages"]
        ForAttr["ForAttributeWithMetadataName(...) transform: new AttributeContext"]
        attributeContexts["AttributeContext"]
        SelOpts["Select GeneratorOptions.Select"]
        options["GeneratorOptions"]
        C1["Combine"]
        SelAttrOpts["Select SelectAttributeAndOptions"]
        attributesAndOptions["AttributeAndOptions"]
        SelAttrGlobs["Select SelectAttributesAndGlobs"]
        attributesAndGlobs["AttributeAndGlob"]
        SelJustGlobs["Select SelectJustGlobs"]
        attributeGlobs["Glob"]
        SelFileInfo["Select SelectFileInfo"]
        fileInfo["AdditionalTextAndGlob"]
        C2["Combine"]
        SelAddTextGlob["Select SelectAdditionalTextAndGlobWithAttributeGlobs"]
        addTextGlobWithAttrGlobs["AdditionalTextAndGlobWithAttributeGlobs"]
        WhereMatch["Where DoesAdditionalTextGlobMatchAnyAttributeGlobs"]
        matchingAddTextGlobs["AdditionalTextAndGlobWithAttributeGlobs"]
        ExtractRes["Select ExtractEmbeddedResourceWithFileInfo"]
        fileAndResource["FileAndGlobs, EmbeddedResource"]
        C3["Combine"]
        SelMatchResAttr["SelectMany SelectMatchingResourceAndAttribute"]
        matchedResourceAndAttribute["AttributeAndResource"]
        CollectRes["Collect"]
        SelectRes["Select Create EquatableImmutableArray"]
        SelCreateDict["Select GroupResourcesByAttributeContext"]
        resourcesByAttributeContextLookup["Dictionary AttributeContext → EquatableImmutableArray&lt;EmbeddedResource&gt;"]
        C4["Combine"]
        SelLookup["Select PerformResourceLookup"]
        generationInput["AttributeOptionsAndResources"]
        RegOut["RegisterSourceOutput"]
        GSC["Generated Source Code"]
    end
    SC == _Every Keypress_ ==> SP
    CF -- _Config Changes_ --> ACOP
    AT == _Whenever Saved_ ==> ATP
    SP == C# Source Code ==> ForAttr
    ForAttr == AttributeContextsCreated ==> attributeContexts
    ACOP -- Project Config --> SelOpts
    SelOpts -- AnalyzerConfigOptions --> options
    attributeContexts ==> C1
    options --> C1
    C1 ==> SelAttrOpts
    SelAttrOpts == AttributesAndOptionsCombined ==> attributesAndOptions
    attributesAndOptions ==> SelAttrGlobs & C4
    SelAttrGlobs == AttributeGlobInfoSelected ==> attributesAndGlobs
    attributesAndGlobs ==> SelJustGlobs
    attributesAndGlobs -- Collect / Select Create EquatableImmutableArray --> C3
    SelJustGlobs == AttributeGlobsSelected ==> attributeGlobs
    ATP == Additional Texts ==> SelFileInfo
    SelFileInfo == FileInfoSelected ==> fileInfo
    fileInfo ==> C2
    attributeGlobs -- Collect / Select Create EquatableImmutableArray --> C2
    C2 == FileInfoAndGlobsCombined ==> SelAddTextGlob
    SelAddTextGlob ==> addTextGlobWithAttrGlobs
    addTextGlobWithAttrGlobs ==> WhereMatch
    WhereMatch == MatchingFilesFiltered ==> matchingAddTextGlobs
    matchingAddTextGlobs ==> ExtractRes
    ExtractRes == EmbeddedResourceExtracted ==> fileAndResource
    fileAndResource ==> C3
    C3 == ResourceAndAllAttributeGlobsCombined ==> SelMatchResAttr
    SelMatchResAttr == MatchingResourceAndAttributeSelected ==> matchedResourceAndAttribute
    matchedResourceAndAttribute ==> CollectRes
    CollectRes --> SelectRes
    SelectRes --> SelCreateDict
    SelCreateDict -- ResourcesGroupedByAttributeContext --> resourcesByAttributeContextLookup
    resourcesByAttributeContextLookup --> C4
    C4 ==> SelLookup
    SelLookup == GenerationInputPrepared ==> generationInput
    generationInput ==> RegOut
    RegOut == GenerateFolderEmbed ==> GSC
    SP@{ shape: procs}
    SC@{ shape: docs}
    CF@{ shape: doc}
    ATP@{ shape: procs}
    AT@{ shape: docs}
    ForAttr@{ shape: procs}
    attributeContexts@{ shape: hex}
    options@{ shape: hex}
    C1@{ shape: trap-b}
    SelAttrOpts@{ shape: procs}
    attributesAndOptions@{ shape: hex}
    SelAttrGlobs@{ shape: procs}
    attributesAndGlobs@{ shape: hex}
    SelJustGlobs@{ shape: procs}
    attributeGlobs@{ shape: hex}
    SelFileInfo@{ shape: procs}
    fileInfo@{ shape: hex}
    C2@{ shape: trap-b}
    SelAddTextGlob@{ shape: procs}
    addTextGlobWithAttrGlobs@{ shape: hex}
    WhereMatch@{ shape: trap-t}
    matchingAddTextGlobs@{ shape: hex}
    ExtractRes@{ shape: procs}
    fileAndResource@{ shape: hex}
    C3@{ shape: trap-b}
    SelMatchResAttr@{ shape: procs}
    matchedResourceAndAttribute@{ shape: hex}
    CollectRes@{ shape: trap-t}
    resourcesByAttributeContextLookup@{ shape: hex}
    C4@{ shape: trap-b}
    SelLookup@{ shape: procs}
    generationInput@{ shape: hex}
    RegOut@{ shape: procs}
    GSC@{ shape: docs}
 ```

