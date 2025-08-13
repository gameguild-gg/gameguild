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
