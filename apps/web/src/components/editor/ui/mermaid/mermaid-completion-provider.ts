import type { languages } from "monaco-editor"

// Detect diagram type from code
function detectDiagramType(code: string): string {
  const firstLine = code.split("\n")[0].trim().toLowerCase()

  if (firstLine.startsWith("flowchart") || firstLine.startsWith("graph")) return "flowchart"
  if (firstLine.startsWith("sequencediagram")) return "sequence"
  if (firstLine.startsWith("classdiagram")) return "class"
  if (firstLine.startsWith("statediagram")) return "state"
  if (firstLine.startsWith("erdiagram")) return "er"
  if (firstLine.startsWith("pie")) return "pie"
  if (firstLine.startsWith("gitgraph")) return "gitgraph"
  if (firstLine.startsWith("requirementdiagram")) return "requirement"
  if (firstLine.startsWith("architecture")) return "architecture"
  if (firstLine.startsWith("c4context")) return "c4context"
  if (firstLine.startsWith("timeline")) return "timeline"
  if (firstLine.startsWith("mindmap")) return "mindmap"

  return "unknown"
}

// Function to check if we are on the first line (where we define the diagram type)
function isFirstLine(position: { lineNumber: number }): boolean {
  return position.lineNumber === 1
}

// Function to filter suggestions based on the current context
function getContextualSuggestions(
  diagramType: string,
  isFirstLinePosition: boolean,
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] {
  const suggestions: languages.CompletionItem[] = []

  // If we are on the first line and no type is defined, show only diagram types
  if (isFirstLinePosition && diagramType === "unknown") {
    return getDiagramTypeCompletions(monaco, range)
  }

  // If we already have a defined type, do not suggest other diagram types
  if (diagramType !== "unknown") {
    // Add current type specific suggestions
    switch (diagramType) {
      case "flowchart":
        suggestions.push(...flowchartCompletions(monaco, range))
        break
      case "sequence":
        suggestions.push(...sequenceCompletions(monaco, range))
        break
      case "class":
        suggestions.push(...classCompletions(monaco, range))
        break
      case "state":
        suggestions.push(...stateCompletions(monaco, range))
        break
      case "er":
        suggestions.push(...erCompletions(monaco, range))
        break
      case "pie":
        suggestions.push(...pieCompletions(monaco, range))
        break
      case "gitgraph":
        suggestions.push(...gitgraphCompletions(monaco, range))
        break
      case "requirement":
        suggestions.push(...requirementCompletions(monaco, range))
        break
      case "architecture":
        suggestions.push(...architectureCompletions(monaco, range))
        break
      case "c4context":
        suggestions.push(...c4contextCompletions(monaco, range))
        break
      case "timeline":
        suggestions.push(...timelineCompletions(monaco, range))
        break
      case "mindmap":
        suggestions.push(...mindmapCompletions(monaco, range))
        break
    }

    // Add only common suggestions (comments), not diagram types
    suggestions.push(...getCommonNonTypeCompletions(monaco, range))
  } else {
    // If type is unidentified, show all options
    suggestions.push(...getAllCompletions(monaco, range))
  }

  return suggestions
}

// Suggestions only for diagram types (first line)
const getDiagramTypeCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "flowchart TD",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "flowchart TD\n    ${1:A}[${2:Start}] --> ${3:B}[${4:Process}]\n    ${3:B} --> ${5:C}[${6:End}]",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a top-down flowchart",
    range,
    sortText: "1", // High priority
  },
  {
    label: "sequenceDiagram",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "sequenceDiagram\n    participant ${1:A}\n    participant ${2:B}\n    ${1:A}->>${2:B}: ${3:Message}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a sequence diagram",
    range,
    sortText: "2",
  },
  {
    label: "classDiagram",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "classDiagram\n    class ${1:Animal} {\n        +String ${2:name}\n        +${3:makeSound()}\n    }",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a class diagram",
    range,
    sortText: "3",
  },
  {
    label: "stateDiagram-v2",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "stateDiagram-v2\n    [*] --> ${1:State1}\n    ${1:State1} --> ${2:State2}\n    ${2:State2} --> [*]",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a state diagram",
    range,
    sortText: "4",
  },
  {
    label: "erDiagram",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText:
      "erDiagram\n    ${1:CUSTOMER} {\n        string ${2:name}\n        int ${3:id}\n    }\n    ${4:ORDER} {\n        int ${5:orderNumber}\n        date ${6:orderDate}\n    }\n    ${1:CUSTOMER} ||--o{ ${4:ORDER} : places",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create an ER diagram",
    range,
    sortText: "5",
  },
  {
    label: "pie",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: 'pie\n    title ${1:Pie Chart Title}\n    showData\n    "${2:Label}" : ${3:value}',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a pie chart",
    range,
    sortText: "6",
  },
  {
    label: "gitGraph",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText:
      'gitGraph\n    commit id: "${1:Initial commit}"\n    branch ${2:develop}\n    checkout ${2:develop}\n    commit id: "${3:Add feature}"\n    checkout main\n    merge ${2:develop}',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a git graph diagram",
    range,
    sortText: "7",
  },
  {
    label: "requirementDiagram",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText:
      "requirementDiagram\n    requirement ${1:test_req} {\n        id: ${2:1}\n        text: ${3:the test text.}\n        risk: ${4:high}\n        verifymethod: ${5:test}\n    }",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a requirement diagram",
    range,
    sortText: "8",
  },
  {
    label: "architecture-beta",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText:
      "architecture-beta\n    group ${1:api}(cloud)[${2:API Layer}]\n    service ${3:db}(database)[${4:Database}] in ${1:api}\n    service ${5:web}(browser)[${6:Web App}]\n    ${5:web}:R -- L:${3:db}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create an architecture diagram",
    range,
    sortText: "9",
  },
  {
    label: "C4Context",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText:
      'C4Context\n    title ${1:System Context diagram}\n    \n    Person(${2:user}, "${3:User}", "${4:A user of the system}")\n    System(${5:system}, "${6:System}", "${7:Description of system}")\n    System_Ext(${8:external}, "${9:External System}", "${10:External system description}")\n    \n    Rel(${2:user}, ${5:system}, "${11:Uses}")',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a C4 Context diagram",
    range,
    sortText: "10",
  },
  {
    label: "timeline",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText:
      "timeline\n    title ${1:Timeline Title}\n    \n    ${2:2021} : ${3:Event 1}\n    ${4:2022} : ${5:Event 2}\n           : ${6:Event 3}\n    ${7:2023} : ${8:Event 4}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a timeline diagram",
    range,
    sortText: "11",
  },
  {
    label: "mindmap",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText:
      "mindmap\n  root((${1:Central Topic}))\n    ${2:Branch 1}\n      ${3:Sub-branch 1}\n      ${4:Sub-branch 2}\n    ${5:Branch 2}\n      ${6:Sub-branch 3}\n        ${7:Detail 1}\n        ${8:Detail 2}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a mindmap diagram",
    range,
    sortText: "12",
  },
]

const c4contextCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "Person",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'Person(${1:id}, "${2:Name}", "${3:Description}")',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a person/user",
    range,
  },
  {
    label: "Person_Ext",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'Person_Ext(${1:id}, "${2:Name}", "${3:Description}")',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define an external person/user",
    range,
  },
  {
    label: "System",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'System(${1:id}, "${2:Name}", "${3:Description}")',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a system",
    range,
  },
  {
    label: "System_Ext",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'System_Ext(${1:id}, "${2:Name}", "${3:Description}")',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define an external system",
    range,
  },
  {
    label: "Rel",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'Rel(${1:from}, ${2:to}, "${3:Label}")',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a relationship",
    range,
  },
  {
    label: "BiRel",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'BiRel(${1:from}, ${2:to}, "${3:Label}")',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a bidirectional relationship",
    range,
  },
  {
    label: "title",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "title ${1:Diagram Title}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Set diagram title",
    range,
  },
]

const timelineCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "title",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "title ${1:Timeline Title}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Set timeline title",
    range,
  },
  {
    label: "section",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "section ${1:Section Name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a timeline section",
    range,
  },
  {
    label: "year event",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:2024} : ${2:Event description}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add a year with event",
    range,
  },
  {
    label: "multiple events",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:2024} : ${2:Event 1}\n       : ${3:Event 2}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add multiple events in same year",
    range,
  },
]

const mindmapCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "root",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "root((${1:Central Topic}))",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define the root node",
    range,
  },
  {
    label: "branch",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:Branch Name}\n  ${2:Sub-branch}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add a branch with sub-branch",
    range,
  },
  {
    label: "icon",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "::icon(${1:fa fa-book})",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add an icon to a node",
    range,
  },
  {
    label: "multi-line",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:Branch}<br/>${2:Second line}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create multi-line text",
    range,
  },
]

// Git Graph specific completions
const gitgraphCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "commit",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'commit id: "${1:commit message}"',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a commit",
    range,
  },
  {
    label: "branch",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "branch ${1:branch_name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a new branch",
    range,
  },
  {
    label: "checkout",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "checkout ${1:branch_name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Switch to a branch",
    range,
  },
  {
    label: "merge",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "merge ${1:branch_name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Merge a branch",
    range,
  },
  {
    label: "cherry-pick",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'cherry-pick id: "${1:commit_id}"',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Cherry-pick a commit",
    range,
  },
  {
    label: "tag",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: 'tag: "${1:tag_name}"',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add a tag",
    range,
  },
]

// Requirement Diagram specific completions
const requirementCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "requirement",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText:
      "requirement ${1:req_name} {\n    id: ${2:1}\n    text: ${3:requirement text}\n    risk: ${4:high}\n    verifymethod: ${5:test}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a requirement",
    range,
  },
  {
    label: "functionalRequirement",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText:
      "functionalRequirement ${1:func_req} {\n    id: ${2:1.1}\n    text: ${3:functional requirement text}\n    risk: ${4:medium}\n    verifymethod: ${5:inspection}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a functional requirement",
    range,
  },
  {
    label: "performanceRequirement",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText:
      "performanceRequirement ${1:perf_req} {\n    id: ${2:1.2}\n    text: ${3:performance requirement text}\n    risk: ${4:low}\n    verifymethod: ${5:demonstration}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a performance requirement",
    range,
  },
  {
    label: "element",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "element ${1:element_name} {\n    type: ${2:simulation}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define an element",
    range,
  },
  {
    label: "satisfies",
    kind: monaco.languages.CompletionItemKind.Operator,
    insertText: "${1:req1} - satisfies -> ${2:element}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Satisfies relationship",
    range,
  },
  {
    label: "derives",
    kind: monaco.languages.CompletionItemKind.Operator,
    insertText: "${1:req1} - derives -> ${2:req2}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Derives relationship",
    range,
  },
  {
    label: "refines",
    kind: monaco.languages.CompletionItemKind.Operator,
    insertText: "${1:req1} - refines -> ${2:req2}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Refines relationship",
    range,
  },
]

// Architecture Diagram specific completions
const architectureCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "group",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "group ${1:group_name}(${2:cloud})[${3:Group Label}]",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a group",
    range,
  },
  {
    label: "service",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "service ${1:service_name}(${2:server})[${3:Service Label}]",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a service",
    range,
  },
  {
    label: "service in group",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "service ${1:service_name}(${2:database})[${3:Service Label}] in ${4:group_name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a service within a group",
    range,
  },
  {
    label: "junction",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "junction ${1:junction_name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a junction",
    range,
  },
  {
    label: "connection",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:service1}:${2:R} -- ${3:L}:${4:service2}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Connect services (R=Right, L=Left, T=Top, B=Bottom)",
    range,
  },
  {
    label: "cloud",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "cloud",
    documentation: "Cloud icon type",
    range,
  },
  {
    label: "server",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "server",
    documentation: "Server icon type",
    range,
  },
  {
    label: "database",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "database",
    documentation: "Database icon type",
    range,
  },
  {
    label: "browser",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "browser",
    documentation: "Browser icon type",
    range,
  },
  {
    label: "phone",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "phone",
    documentation: "Phone icon type",
    range,
  },
]

// Common suggestions that do not include diagram types
const getCommonNonTypeCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "%%",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "%% ${1:comment}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add a comment",
    range,
    sortText: "z", // Low priority
  },
]

// All suggestions (for unknown context)
const getAllCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  ...getDiagramTypeCompletions(monaco, range),
  ...getCommonNonTypeCompletions(monaco, range),
]

// Flowchart specific completions
const flowchartCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "subgraph",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "subgraph ${1:title}\n    ${2:content}\nend",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Create a subgraph container",
    range,
    sortText: "a", // High priority for specific keywords
  },
  {
    label: "A[Rectangle]",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A}[${2:Rectangle}]",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Rectangle node shape",
    range,
    sortText: "b",
  },
  {
    label: "A(Round)",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A}(${2:Round})",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Round node shape",
    range,
    sortText: "b",
  },
  {
    label: "A{Diamond}",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A}{${2:Diamond}}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Diamond decision node",
    range,
    sortText: "b",
  },
  {
    label: "A((Circle))",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A}((${2:Circle}))",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Circle node shape",
    range,
    sortText: "b",
  },
  {
    label: "TD",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "TD",
    documentation: "Top Down direction",
    range,
    sortText: "c",
  },
  {
    label: "LR",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "LR",
    documentation: "Left to Right direction",
    range,
    sortText: "c",
  },
]

// Sequence diagram specific completions
const sequenceCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "participant",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "participant ${1:Name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a participant",
    range,
  },
  {
    label: "actor",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "actor ${1:Name}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define an actor",
    range,
  },
  {
    label: "A->>B: Message",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A}->>${2:B}: ${3:Message}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Solid arrow message",
    range,
  },
  {
    label: "A-->>B: Message",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A}-->>${2:B}: ${3:Message}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Dotted arrow message",
    range,
  },
  {
    label: "activate",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "activate ${1:Participant}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Activate a participant",
    range,
  },
  {
    label: "deactivate",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "deactivate ${1:Participant}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Deactivate a participant",
    range,
  },
  {
    label: "note over",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "note over ${1:Participant}: ${2:Note text}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add a note over participant",
    range,
  },
  {
    label: "loop",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "loop ${1:condition}\n    ${2:messages}\nend",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Loop block",
    range,
  },
]

// Class diagram specific completions
const classCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "class",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "class ${1:ClassName} {\n    ${2:+attribute: type}\n    ${3:+method()}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a class",
    range,
  },
  {
    label: "interface",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "class ${1:InterfaceName} {\n    <<interface>>\n    ${2:+method()}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define an interface",
    range,
  },
  {
    label: "abstract",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "class ${1:AbstractClass} {\n    <<abstract>>\n    ${2:+method()}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define an abstract class",
    range,
  },
  {
    label: "A --|> B",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A} --|> ${2:B}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Inheritance relationship",
    range,
  },
  {
    label: "A --* B",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A} --* ${2:B}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Composition relationship",
    range,
  },
  {
    label: "A --o B",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A} --o ${2:B}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Aggregation relationship",
    range,
  },
  {
    label: "+public",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "+",
    documentation: "Public visibility",
    range,
  },
  {
    label: "-private",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "-",
    documentation: "Private visibility",
    range,
  },
  {
    label: "#protected",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "#",
    documentation: "Protected visibility",
    range,
  },
]

// State diagram specific completions
const stateCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "[*]",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "[*]",
    documentation: "Start/End state",
    range,
  },
  {
    label: "state",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "state ${1:StateName}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define a state",
    range,
  },
  {
    label: "A --> B",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A} --> ${2:B}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "State transition",
    range,
  },
  {
    label: "A --> B : event",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:A} --> ${2:B} : ${3:event}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "State transition with event",
    range,
  },
  {
    label: "state composite",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "state ${1:CompositeState} {\n    ${2:[*]} --> ${3:SubState}\n    ${3:SubState} --> ${4:[*]}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Composite state",
    range,
  },
  {
    label: "fork",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "state ${1:fork_state} <<fork>>",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Fork state",
    range,
  },
  {
    label: "join",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "state ${1:join_state} <<join>>",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Join state",
    range,
  },
]

// ER diagram specific completions
const erCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "CUSTOMER",
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: "${1:CUSTOMER} {\n    ${2:string} ${3:name}\n    ${4:int} ${5:id}\n}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Define an entity",
    range,
  },
  {
    label: "||--o{",
    kind: monaco.languages.CompletionItemKind.Operator,
    insertText: "||--o{",
    documentation: "One to many relationship",
    range,
  },
  {
    label: "||--|{",
    kind: monaco.languages.CompletionItemKind.Operator,
    insertText: "||--|{",
    documentation: "One to many identifying relationship",
    range,
  },
  {
    label: "||--||",
    kind: monaco.languages.CompletionItemKind.Operator,
    insertText: "||--||",
    documentation: "One to one relationship",
    range,
  },
  {
    label: "}|--|{",
    kind: monaco.languages.CompletionItemKind.Operator,
    insertText: "}|--|{",
    documentation: "Many to many relationship",
    range,
  },
  {
    label: "string",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "string",
    documentation: "String data type",
    range,
  },
  {
    label: "int",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "int",
    documentation: "Integer data type",
    range,
  },
  {
    label: "date",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "date",
    documentation: "Date data type",
    range,
  },
]

// Pie chart specific completions
const pieCompletions = (
  monaco: typeof import("monaco-editor"),
  range: import("monaco-editor").IRange,
): languages.CompletionItem[] => [
  {
    label: "title",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "title ${1:Chart Title}",
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Set chart title",
    range,
  },
  {
    label: "showData",
    kind: monaco.languages.CompletionItemKind.Keyword,
    insertText: "showData",
    documentation: "Show data values on chart",
    range,
  },
  {
    label: '"Label" : value',
    kind: monaco.languages.CompletionItemKind.Snippet,
    insertText: '"${1:Label}" : ${2:value}',
    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    documentation: "Add data point",
    range,
  },
]

export function createMermaidCompletionProvider(
  monaco: typeof import("monaco-editor"),
): languages.CompletionItemProvider {
  return {
    provideCompletionItems: (model, position) => {
      const code = model.getValue()
      const diagramType = detectDiagramType(code)
      const isFirstLinePosition = isFirstLine(position)

      const word = model.getWordUntilPosition(position)
      const range: import("monaco-editor").IRange = {
        startLineNumber: position.lineNumber,
        endLineNumber: position.lineNumber,
        startColumn: word.startColumn,
        endColumn: word.endColumn,
      }

      // Use new contextual function for hierarchical suggestions
      const suggestions = getContextualSuggestions(diagramType, isFirstLinePosition, monaco, range)

      return { suggestions }
    },
  }
}
