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
    "C4Context", // Added C4 Context support
    "C4Container",
    "C4Component",
    "xychart-beta", // Added XY Chart support
    "radar-beta", // Added Radar Chart support
    "sankey-beta", // Added Sankey Diagram support

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

    // Git graph keywords
    "gitGraph",
    "commit",
    "branch",
    "checkout",
    "merge",
    "cherry-pick",
    "reset",
    "revert",
    "tag",

    // Requirement diagram keywords
    "requirementDiagram",
    "requirement",
    "functionalRequirement",
    "performanceRequirement",
    "interfaceRequirement",
    "physicalRequirement",
    "designConstraint",
    "element",
    "satisfies",
    "derives",
    "refines",
    "traces",
    "verifies",
    "contains",
    "copies",
    "risk",
    "verifymethod",
    "test",
    "inspection",
    "analysis",
    "demonstration",

    // Architecture diagram keywords
    "architecture",
    "architecture-beta",
    "group",
    "service",
    "junction",
    "cloud",
    "server",
    "database",
    "browser",
    "phone",
    "disk",
    "person",
    "external",

    "Person",
    "Person_Ext",
    "System",
    "System_Ext",
    "SystemDb",
    "SystemQueue",
    "Container",
    "Container_Ext",
    "ContainerDb",
    "ContainerQueue",
    "Component",
    "ComponentDb",
    "ComponentQueue",
    "Rel",
    "BiRel",
    "Rel_Up",
    "Rel_Down",
    "Rel_Left",
    "Rel_Right",

    "section",
    "title",

    "root",
    "icon",

    "x-axis",
    "y-axis",
    "bar",
    "line",

    "radar",

    "quadrant-1",
    "quadrant-2",
    "quadrant-3",
    "quadrant-4",

    "sankey",
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
        /^(graph|flowchart|sequenceDiagram|classDiagram|stateDiagram|erDiagram|journey|gantt|pie|gitgraph|gitGraph|mindmap|timeline|quadrantChart|requirement|requirementDiagram|architecture|architecture-beta|c4Context|c4Container|c4Component|c4Dynamic|c4Deployment|C4Context|C4Container|C4Component|xychart-beta|radar-beta|sankey-beta)\b/,
        "keyword.diagram",
      ],

      // Direction keywords
      [/\b(TD|TB|BT|RL|LR)\b/, "keyword.direction"],

      // Keywords
      [
        /\b(subgraph|end|participant|actor|activate|deactivate|note|over|left|right|of|loop|alt|else|opt|par|and|critical|break|rect|autonumber|class|namespace|interface|abstract|enum|public|private|protected|static|final|state|fork|join|choice|entity|relationship|title|dateFormat|axisFormat|section|excludes|includes|todayMarker|journey|commit|branch|checkout|merge|requirementDiagram|requirement|functionalRequirement|performanceRequirement|interfaceRequirement|physicalRequirement|designConstraint|element|satisfies|derives|refines|traces|verifies|contains|copies|risk|verifymethod|test|inspection|analysis|demonstration|architecture|architecture-beta|group|service|junction|cloud|server|database|browser|phone|disk|person|external|Person|Person_Ext|System|System_Ext|SystemDb|SystemQueue|Container|Container_Ext|ContainerDb|ContainerQueue|Component|ComponentDb|ComponentQueue|Rel|BiRel|Rel_Up|Rel_Down|Rel_Left|Rel_Right|root|icon|x-axis|y-axis|bar|line|radar|quadrant-1|quadrant-2|quadrant-3|quadrant-4|sankey)\b/,
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

      // Git graph specific
      [/\b(commit|branch|checkout|merge|cherry-pick|reset|revert|tag)\b/, "keyword.git"],
      [/\bid:\s*"[^"]*"/, "string.git.id"],

      // Requirement diagram specific
      [
        /\b(requirement|functionalRequirement|performanceRequirement|interfaceRequirement|physicalRequirement|designConstraint|element)\b/,
        "keyword.requirement",
      ],
      [/\b(satisfies|derives|refines|traces|verifies|contains|copies)\b/, "keyword.requirement.relation"],
      [/\b(risk|verifymethod|id|text|type)\b/, "keyword.requirement.property"],
      [/\b(high|medium|low|test|inspection|analysis|demonstration)\b/, "keyword.requirement.value"],

      // Architecture diagram specific
      [/\b(group|service|junction)\b/, "keyword.architecture"],
      [/\b(cloud|server|database|browser|phone|disk|person|external|simulation)\b/, "keyword.architecture.type"],
      [/$$[^)]+$$\[[^\]]+\]/, "string.architecture.definition"],

      [
        /\b(Person|Person_Ext|System|System_Ext|SystemDb|SystemQueue|Container|Container_Ext|ContainerDb|ContainerQueue|Component|ComponentDb|ComponentQueue)\b/,
        "keyword.c4",
      ],
      [/\b(Rel|BiRel|Rel_Up|Rel_Down|Rel_Left|Rel_Right)\b/, "keyword.c4.relation"],

      [/^\s*\d{4}\s*:/, "number.timeline.year"],
      [/^\s*section\s+.+/, "keyword.timeline.section"],

      [/^\s*root$$$$.+$$$$/, "keyword.mindmap.root"],
      [/::icon$$[^)]+$$/, "string.mindmap.icon"],

      [/^xychart-beta/, "keyword.xychart"],
      [/^\s*(x-axis|y-axis)\s+/, "keyword.xychart.axis"],
      [/^\s*(bar|line)\s+/, "keyword.xychart.type"],

      [/^radar-beta/, "keyword.radar"],
      [/^\s*"[^"]+"\s*:\s*\d*\.?\d+/, "string.radar.data"],

      [/^quadrantChart/, "keyword.quadrant"],
      [/^\s*(quadrant-[1-4])\s+/, "keyword.quadrant.section"],
      [/^\s*[A-Za-z\s]+:\s*\[\d*\.?\d+,\s*\d*\.?\d+\]/, "string.quadrant.point"],

      [/^sankey-beta/, "keyword.sankey"],
      [/^[^,]+,[^,]+,\d+(\.\d+)?/, "string.sankey.flow"],
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
    { token: "keyword.git", foreground: "f66a0a", fontStyle: "bold" },
    { token: "string.git.id", foreground: "22863a" },
    { token: "keyword.requirement", foreground: "6f42c1", fontStyle: "bold" },
    { token: "keyword.requirement.relation", foreground: "d73a49" },
    { token: "keyword.requirement.property", foreground: "005cc5" },
    { token: "keyword.requirement.value", foreground: "22863a" },
    { token: "keyword.architecture", foreground: "e36209", fontStyle: "bold" },
    { token: "keyword.architecture.type", foreground: "6f42c1" },
    { token: "string.architecture.definition", foreground: "032f62" },
    { token: "keyword.c4", foreground: "0366d6", fontStyle: "bold" },
    { token: "keyword.c4.relation", foreground: "d73a49" },
    { token: "number.timeline.year", foreground: "005cc5", fontStyle: "bold" },
    { token: "keyword.timeline.section", foreground: "6f42c1", fontStyle: "bold" },
    { token: "keyword.mindmap.root", foreground: "d73a49", fontStyle: "bold" },
    { token: "string.mindmap.icon", foreground: "22863a" },
    { token: "keyword.xychart", foreground: "d73a49", fontStyle: "bold" },
    { token: "keyword.xychart.axis", foreground: "6f42c1", fontStyle: "bold" },
    { token: "keyword.xychart.type", foreground: "005cc5" },
    { token: "keyword.radar", foreground: "d73a49", fontStyle: "bold" },
    { token: "string.radar.data", foreground: "22863a" },
    { token: "keyword.quadrant", foreground: "d73a49", fontStyle: "bold" },
    { token: "keyword.quadrant.section", foreground: "6f42c1", fontStyle: "bold" },
    { token: "string.quadrant.point", foreground: "22863a" },
    { token: "keyword.sankey", foreground: "d73a49", fontStyle: "bold" },
    { token: "string.sankey.flow", foreground: "22863a" },
  ],
  colors: {
    "editor.background": "#ffffff",
    "editor.foreground": "#24292e",
    "editorLineNumber.foreground": "#959da5",
    "editorLineNumber.activeForeground": "#24292e",
  },
}
