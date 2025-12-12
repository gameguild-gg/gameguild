export type SupportedLanguage = 
  | "javascript" 
  | "typescript" 
  | "python" 
  | "lua" 
  | "c" 
  | "cpp"
  | "html"
  | "css"
  | "markdown"
  | "java"
  | "go"
  | "rust"
  | "php"
  | "ruby"
  | "swift"
  | "kotlin"
  | "csharp"
  | "sql"
  | "bash"
  | "powershell"
  | "r"
  | "scala"
  | "dart"
  | "json"
  | "yaml"
  | "xml"

export type EditorMode = 
  | "execution"  // Executa código com console na direita
  | "test"       // Modo de testes

export type ShikiTheme = 
  | "github"           // GitHub theme
  | "github-default"   // GitHub Default theme
  | "github-dimmed"    // GitHub Dimmed theme
  | "plus"             // Plus theme

export interface CodeFile {
  id: string
  name: string
  content: string
  language: SupportedLanguage
  isMain: boolean
  isVisible: boolean
  path: string // Caminho completo incluindo pastas, ex: "src/components/Button.tsx"
}

export interface FileTreeFolder {
  id: string
  name: string
  path: string
  isExpanded: boolean
  children: (CodeFile | FileTreeFolder)[]
  type: "folder"
}

export type FileTreeItem = CodeFile | FileTreeFolder

export type PanelType = "explorer" | "editor" | "output"

export type EditorInstance = "multiple" | "unique"

export type AspectRatio = "2:1" | "1:1" | "1:2"

// Grid dimensions based on aspect ratio:
// "2:1" (landscape) = 24x12 (24 cols x 12 rows)
// "1:1" (square) = 12x12 (12 cols x 12 rows)
// "1:2" (portrait) = 12x24 (12 cols x 24 rows)

export interface PanelConfig {
  id: string
  type: PanelType
  row: number // 0 to (rows-1), depends on display aspect ratio
  col: number // 0 to (cols-1), depends on display aspect ratio
  rowSpan: number // 1 to rows, depends on display aspect ratio
  colSpan: number // 1 to cols, depends on display aspect ratio
  editorInstance?: EditorInstance // Apenas para painéis tipo "editor"
}

export interface DisplayConfig {
  id: string // "display-1", "display-2", etc
  name: string // Título customizável pelo usuário
  aspectRatio: AspectRatio // Proporção da área útil do display
  panels: PanelConfig[]
  uniqueOpenTabs?: string[] // Abas abertas específicas deste display (quando editor é unique)
  uniqueActiveFileId?: string // Arquivo ativo específico deste display (quando editor é unique)
}

export interface LayoutConfig {
  displays: DisplayConfig[]
  activeDisplayId: string
  editMode: boolean
}

export interface TestCase {
  id: string
  name: string
  type: "simple" | "function" | "custom"
  input?: string
  expectedOutput?: string
  args?: any[]
  expectedReturn?: any
  customCode?: string
  enabled: boolean
}

export interface TestResult {
  testId: string
  passed: boolean
  actual: string
  expected: string
  error?: string
}

export type CodeStudioMode = "execution" | "test"

export interface CodeStudioData {
  id: string // ID único do node code-studio
  files: CodeFile[]
  folders: FileTreeFolder[]
  openTabs: string[] // IDs dos arquivos abertos em abas
  activeFileId?: string
  mode: CodeStudioMode
  language: SupportedLanguage
  title?: string
  caption?: string
  
  // Configurações de UI
  readonly?: boolean
  showLineNumbers?: boolean
  fontSize?: number
  theme?: "light" | "dark" | "system"
  shikiTheme?: ShikiTheme // Tema do Shiki (syntax highlighting)
  
  // Layout customizável
  layout?: LayoutConfig
  
  // Configurações de execução
  clearOnRun?: boolean
  autoRun?: boolean
  
  // Testes
  testCases?: Record<string, TestCase[]>
  showTests?: boolean
}

export interface ModeConfig {
  id: CodeStudioMode
  label: string
  description: string
  icon: string
  supportedLanguages: SupportedLanguage[]
}

export const MODE_CONFIGS: Record<CodeStudioMode, ModeConfig> = {
  execution: {
    id: "execution",
    label: "Execution Mode",
    description: "Write and execute code with real-time output console. Perfect for development and debugging.",
    icon: "Play",
    supportedLanguages: ["javascript", "typescript", "python", "lua", "c", "cpp"],
  },
  test: {
    id: "test",
    label: "Test Mode",
    description: "Run automated test cases against your code. Validate inputs, outputs, and expected behaviors.",
    icon: "TestTube",
    supportedLanguages: ["javascript", "typescript", "python", "lua", "c", "cpp"],
  },
}

export const LANGUAGE_CONFIGS: Record<
  SupportedLanguage,
  {
    label: string
    monacoLanguage: string
    defaultExtension: string
    supportsExecution: boolean
    defaultTemplate: string
  }
> = {
  javascript: {
    label: "JavaScript",
    monacoLanguage: "javascript",
    defaultExtension: ".js",
    supportsExecution: true,
    defaultTemplate: '// Write your JavaScript code here\nconsole.log("Hello, World!");',
  },
  typescript: {
    label: "TypeScript",
    monacoLanguage: "typescript",
    defaultExtension: ".ts",
    supportsExecution: true,
    defaultTemplate: '// Write your TypeScript code here\nconsole.log("Hello, World!");',
  },
  python: {
    label: "Python",
    monacoLanguage: "python",
    defaultExtension: ".py",
    supportsExecution: true,
    defaultTemplate: '# Write your Python code here\nprint("Hello, World!")',
  },
  lua: {
    label: "Lua",
    monacoLanguage: "lua",
    defaultExtension: ".lua",
    supportsExecution: true,
    defaultTemplate: '-- Write your Lua code here\nprint("Hello, World!")',
  },
  c: {
    label: "C",
    monacoLanguage: "c",
    defaultExtension: ".c",
    supportsExecution: true,
    defaultTemplate: '#include <stdio.h>\n\nint main() {\n    printf("Hello, World!\\n");\n    return 0;\n}',
  },
  cpp: {
    label: "C++",
    monacoLanguage: "cpp",
    defaultExtension: ".cpp",
    supportsExecution: true,
    defaultTemplate:
      '#include <iostream>\n\nint main() {\n    std::cout << "Hello, World!" << std::endl;\n    return 0;\n}',
  },
  html: {
    label: "HTML",
    monacoLanguage: "html",
    defaultExtension: ".html",
    supportsExecution: false,
    defaultTemplate: '<!DOCTYPE html>\n<html>\n<head>\n    <title>Page Title</title>\n</head>\n<body>\n    <h1>Hello, World!</h1>\n</body>\n</html>',
  },
  css: {
    label: "CSS",
    monacoLanguage: "css",
    defaultExtension: ".css",
    supportsExecution: false,
    defaultTemplate: 'body {\n    font-family: sans-serif;\n    margin: 20px;\n}\n\nh1 {\n    color: #333;\n}',
  },
  markdown: {
    label: "Markdown",
    monacoLanguage: "markdown",
    defaultExtension: ".md",
    supportsExecution: false,
    defaultTemplate: '# Hello, World!\n\nThis is a **markdown** document.\n\n- Item 1\n- Item 2\n- Item 3',
  },
  java: {
    label: "Java",
    monacoLanguage: "java",
    defaultExtension: ".java",
    supportsExecution: false,
    defaultTemplate: 'public class Main {\n    public static void main(String[] args) {\n        System.out.println("Hello, World!");\n    }\n}',
  },
  go: {
    label: "Go",
    monacoLanguage: "go",
    defaultExtension: ".go",
    supportsExecution: false,
    defaultTemplate: 'package main\n\nimport "fmt"\n\nfunc main() {\n    fmt.Println("Hello, World!")\n}',
  },
  rust: {
    label: "Rust",
    monacoLanguage: "rust",
    defaultExtension: ".rs",
    supportsExecution: false,
    defaultTemplate: 'fn main() {\n    println!("Hello, World!");\n}',
  },
  php: {
    label: "PHP",
    monacoLanguage: "php",
    defaultExtension: ".php",
    supportsExecution: false,
    defaultTemplate: '<?php\necho "Hello, World!";\n?>',
  },
  ruby: {
    label: "Ruby",
    monacoLanguage: "ruby",
    defaultExtension: ".rb",
    supportsExecution: false,
    defaultTemplate: '# Write your Ruby code here\nputs "Hello, World!"',
  },
  swift: {
    label: "Swift",
    monacoLanguage: "swift",
    defaultExtension: ".swift",
    supportsExecution: false,
    defaultTemplate: 'import Foundation\n\nprint("Hello, World!")',
  },
  kotlin: {
    label: "Kotlin",
    monacoLanguage: "kotlin",
    defaultExtension: ".kt",
    supportsExecution: false,
    defaultTemplate: 'fun main() {\n    println("Hello, World!")\n}',
  },
  csharp: {
    label: "C#",
    monacoLanguage: "csharp",
    defaultExtension: ".cs",
    supportsExecution: false,
    defaultTemplate: 'using System;\n\nclass Program {\n    static void Main() {\n        Console.WriteLine("Hello, World!");\n    }\n}',
  },
  sql: {
    label: "SQL",
    monacoLanguage: "sql",
    defaultExtension: ".sql",
    supportsExecution: false,
    defaultTemplate: '-- Write your SQL code here\nSELECT \'Hello, World!\' AS greeting;',
  },
  bash: {
    label: "Bash",
    monacoLanguage: "shell",
    defaultExtension: ".sh",
    supportsExecution: false,
    defaultTemplate: '#!/bin/bash\necho "Hello, World!"',
  },
  powershell: {
    label: "PowerShell",
    monacoLanguage: "powershell",
    defaultExtension: ".ps1",
    supportsExecution: false,
    defaultTemplate: '# Write your PowerShell code here\nWrite-Host "Hello, World!"',
  },
  r: {
    label: "R",
    monacoLanguage: "r",
    defaultExtension: ".r",
    supportsExecution: false,
    defaultTemplate: '# Write your R code here\nprint("Hello, World!")',
  },
  scala: {
    label: "Scala",
    monacoLanguage: "scala",
    defaultExtension: ".scala",
    supportsExecution: false,
    defaultTemplate: 'object Main extends App {\n  println("Hello, World!")\n}',
  },
  dart: {
    label: "Dart",
    monacoLanguage: "dart",
    defaultExtension: ".dart",
    supportsExecution: false,
    defaultTemplate: 'void main() {\n  print(\'Hello, World!\');\n}',
  },
  json: {
    label: "JSON",
    monacoLanguage: "json",
    defaultExtension: ".json",
    supportsExecution: false,
    defaultTemplate: '{\n  "message": "Hello, World!",\n  "version": "1.0.0"\n}',
  },
  yaml: {
    label: "YAML",
    monacoLanguage: "yaml",
    defaultExtension: ".yaml",
    supportsExecution: false,
    defaultTemplate: 'message: Hello, World!\nversion: 1.0.0\nitems:\n  - first\n  - second\n  - third',
  },
  xml: {
    label: "XML",
    monacoLanguage: "xml",
    defaultExtension: ".xml",
    supportsExecution: false,
    defaultTemplate: '<?xml version="1.0" encoding="UTF-8"?>\n<root>\n  <message>Hello, World!</message>\n</root>',
  },
}

// Helper function to detect language from file extension
export function getLanguageFromExtension(filename: string): SupportedLanguage {
  const ext = filename.split('.').pop()?.toLowerCase()
  
  // Casos especiais com múltiplas extensões
  switch (ext) {
    case 'js': return 'javascript'
    case 'ts':
    case 'tsx': return 'typescript'
    case 'py': return 'python'
    case 'lua': return 'lua'
    case 'c':
    case 'h': return 'c'
    case 'cpp':
    case 'cc':
    case 'cxx':
    case 'hpp': return 'cpp'
    case 'html':
    case 'htm': return 'html'
    case 'css': return 'css'
    case 'md':
    case 'markdown': return 'markdown'
    case 'java': return 'java'
    case 'go': return 'go'
    case 'rs': return 'rust'
    case 'php': return 'php'
    case 'rb': return 'ruby'
    case 'swift': return 'swift'
    case 'kt':
    case 'kts': return 'kotlin'
    case 'cs': return 'csharp'
    case 'sql': return 'sql'
    case 'sh':
    case 'bash': return 'bash'
    case 'ps1': return 'powershell'
    case 'r': return 'r'
    case 'scala':
    case 'sc': return 'scala'
    case 'dart': return 'dart'
    case 'json': return 'json'
    case 'yaml':
    case 'yml': return 'yaml'
    case 'xml': return 'xml'
    default: return 'javascript'
  }
}

// Helper function to get Monaco language from our language type
export function getMonacoLanguage(language: SupportedLanguage): string {
  return LANGUAGE_CONFIGS[language].monacoLanguage
}

// Shiki theme configurations
export const SHIKI_THEME_CONFIGS: Record<ShikiTheme, { label: string; dark: string; light: string }> = {
  "github": {
    label: "GitHub",
    dark: "github-dark",
    light: "github-light",
  },
  "github-default": {
    label: "GitHub Default",
    dark: "github-dark-default",
    light: "github-light-default",
  },
  "github-dimmed": {
    label: "GitHub Dimmed",
    dark: "github-dark-dimmed",
    light: "github-light-default",
  },
  "plus": {
    label: "Plus",
    dark: "dark-plus",
    light: "light-plus",
  },
}

// Helper to get Shiki theme name based on color mode
export function getShikiThemeName(shikiTheme: ShikiTheme, isDark: boolean): string {
  const config = SHIKI_THEME_CONFIGS[shikiTheme]
  return isDark ? config.dark : config.light
}
