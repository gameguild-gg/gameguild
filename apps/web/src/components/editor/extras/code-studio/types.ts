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
}

// Helper function to detect language from file extension
export function getLanguageFromExtension(filename: string): SupportedLanguage {
  const ext = filename.split('.').pop()?.toLowerCase()
  
  const languageEntry = Object.entries(LANGUAGE_CONFIGS).find(
    ([_, config]) => config.defaultExtension === `.${ext}`
  )
  
  return languageEntry ? (languageEntry[0] as SupportedLanguage) : 'javascript'
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
