import type { languages } from "monaco-editor"

// Mermaid language configuration
export const mermaidLanguageConfig: languages.LanguageConfiguration = {
  comments: {
    lineComment: "%%",
  },
  brackets: [
    ["{", "}"],
    ["[", "]"],
    ["(", ")"],
  ],
  autoClosingPairs: [
    { open: "{", close: "}" },
    { open: "[", close: "]" },
    { open: "(", close: ")" },
    { open: '"', close: '"' },
    { open: "'", close: "'" },
  ],
  surroundingPairs: [
    { open: "{", close: "}" },
    { open: "[", close: "]" },
    { open: "(", close: ")" },
    { open: '"', close: '"' },
    { open: "'", close: "'" },
  ],
}

// Mermaid syntax highlighting rules
export const mermaidTokensProvider: languages.IMonarchLanguage = {
  defaultToken: "",
  tokenPostfix: ".mermaid",

  keywords: [
    // Diagram types
    "graph",
    "flowchart",
    "sequenceDiagram",
    "classDiagram",
    "stateDiagram",
    "erDiagram",
    "journey",
    "gantt",
    "pie",
    "gitgraph",
    "mindmap",
    "timeline",
    "quadrantChart",
    "requirement",
    "c4Context",
    "c4Container",
    "c4Component",
    "c4Dynamic",
    "c4Deployment",

    // Directions
    "TD",
    "TB",
    "BT",
    "RL",
    "LR",

    // Flowchart keywords
    "subgraph",
    "end",

    // Sequence diagram keywords
    "participant",
    "actor",
    "activate",
    "deactivate",
    "note",
    "over",
    "left",
    "right",
    "of",
    "loop",
    "alt",
    "else",
    "opt",
    "par",
    "and",
    "critical",
    "break",
    "rect",
    "autonumber",

    // Class diagram keywords
    "class",
    "namespace",
    "interface",
    "abstract",
    "enum",
    "public",
    "private",
    "protected",
    "static",
    "final",

    // State diagram keywords
    "state",
    "fork",
    "join",
    "choice",
    "start",
    "end",
    "composite",
    "concurrent",
    "history",
    "deep",
    "shallow",

    // ER diagram keywords
    "entity",
    "relationship",
    "attributes",
    "key",
    "weak",
    "identifying",
    "non-identifying",
    "one",
    "many",
    "zero",
    "optional",

    // Pie chart specific keywords
    "pie",
    "showData",
    "title",
  ],

  operators: [
    "-->",
    "---",
    "-.->",
    "-.-",
    "==>",
    "==",
    "~~>",
    "~",
    "->",
    "-",
    ":",
    "|",
    "||",
    "o|",
    "}|",
    "}o",
    "||--o{",
    "||--|{",
    "<<",
    ">>",
    "*--",
    "*--*",
    "o--",
    "o--o",
    "<|--",
    "<|..",
    "|>",
    "..|>",
    // ER diagram relationship operators
    "||--o{",
    "||--|{",
    "}|--||",
    "}o--||",
    "||--||",
    "}|..|{",
    "}o..|{",
    "||..|{",
    "||..||",
    // State diagram operators
    "[*]",
    "-->",
    ": ",
  ],

  symbols: /[=><!~?:&|+\-*/^%]+/,

  tokenizer: {
    root: [
      // Comments
      [/%%.*$/, "comment"],

      // Diagram type declarations
      [
        /^(graph|flowchart|sequenceDiagram|classDiagram|stateDiagram|erDiagram|journey|gantt|pie|gitgraph|mindmap|timeline|quadrantChart|requirement|c4Context|c4Container|c4Component|c4Dynamic|c4Deployment)\b/,
        "keyword.diagram",
      ],

      // Direction keywords
      [/\b(TD|TB|BT|RL|LR)\b/, "keyword.direction"],

      // Keywords
      [
        /\b(subgraph|end|participant|actor|activate|deactivate|note|over|left|right|of|loop|alt|else|opt|par|and|critical|break|rect|autonumber|class|namespace|interface|abstract|enum|public|private|protected|static|final|state|fork|join|choice|entity|relationship|title|dateFormat|axisFormat|section|excludes|includes|todayMarker|journey|commit|branch|checkout|merge)\b/,
        "keyword",
      ],

      // Arrows and connections
      [
        /-->|---|-.->|-.-|==>|==|~~>|~|->|-|\|\||o\||}\||}o|\|\|--o\{|\|\|--\|\{|<<|>>|\*--|\*--\*|o--|o--o|<\|--|<\|\.\.|\|>|\.\.\|>/,
        "operator.arrow",
      ],

      // Node shapes
      [/[[\](){}]/, "delimiter.bracket"],

      // Strings
      [/"([^"\\]|\\.)*$/, "string.invalid"],
      [/"/, "string", "@string_double"],
      [/'([^'\\]|\\.)*$/, "string.invalid"],
      [/'/, "string", "@string_single"],

      // Node IDs and labels
      [/[A-Za-z_][A-Za-z0-9_]*/, "identifier"],

      // Numbers
      [/\d+/, "number"],

      // Whitespace
      [/[ \t\r\n]+/, ""],

      // Other symbols
      [/@symbols/, "operator"],

      [/\b(CUSTOMER|ORDER|PRODUCT|PERSON|COMPANY|ADDRESS)\b/, "type.entity"],
      [/\b(has|belongs_to|contains|owns|manages|works_for)\b/, "keyword.relationship"],
      [
        /\|\|--o\{|\|\|--\|\{|\}\|--\|\||\}o--\|\||\|\|--\|\||\}\|\.\.|\{|\}o\.\.|\{|\|\|\.\.|\{|\|\|\.\.|\|\|/,
        "operator.er",
      ],

      [/\[\*\]/, "keyword.state.start"],
      [/\bstate\s+[A-Za-z_][A-Za-z0-9_]*\s+as\s+[A-Za-z_][A-Za-z0-9_]*/, "keyword.state.definition"],
      [/\b(fork|join|choice|start|end|composite|concurrent|history|deep|shallow)\b/, "keyword.state"],

      [/^pie\s+(title\s+.+)?/, "keyword.pie"],
      [/^\s*"[^"]+"\s*:\s*\d+(\.\d+)?/, "string.pie.data"],
      [/\b(showData)\b/, "keyword.pie.option"],
    ],

    string_double: [
      [/[^\\"]+/, "string"],
      [/\\./, "string.escape"],
      [/"/, "string", "@pop"],
    ],

    string_single: [
      [/[^\\']+/, "string"],
      [/\\./, "string.escape"],
      [/'/, "string", "@pop"],
    ],
  },
}

// Theme for Mermaid syntax highlighting
export const mermaidTheme = {
  base: "vs" as const,
  inherit: true,
  rules: [
    { token: "comment", foreground: "6a737d", fontStyle: "italic" },
    { token: "keyword.diagram", foreground: "d73a49", fontStyle: "bold" },
    { token: "keyword.direction", foreground: "005cc5", fontStyle: "bold" },
    { token: "keyword", foreground: "d73a49" },
    { token: "operator.arrow", foreground: "e36209", fontStyle: "bold" },
    { token: "string", foreground: "032f62" },
    { token: "string.escape", foreground: "e36209" },
    { token: "identifier", foreground: "6f42c1" },
    { token: "number", foreground: "005cc5" },
    { token: "delimiter.bracket", foreground: "24292e" },
    { token: "type.entity", foreground: "22863a", fontStyle: "bold" },
    { token: "keyword.relationship", foreground: "b31d28" },
    { token: "operator.er", foreground: "e36209", fontStyle: "bold" },
    { token: "keyword.state.start", foreground: "d73a49", fontStyle: "bold" },
    { token: "keyword.state.definition", foreground: "6f42c1" },
    { token: "keyword.state", foreground: "005cc5" },
    { token: "keyword.pie", foreground: "d73a49", fontStyle: "bold" },
    { token: "string.pie.data", foreground: "22863a" },
    { token: "keyword.pie.option", foreground: "6f42c1" },
  ],
  colors: {
    "editor.background": "#ffffff",
    "editor.foreground": "#24292e",
    "editorLineNumber.foreground": "#959da5",
    "editorLineNumber.activeForeground": "#24292e",
  },
}
