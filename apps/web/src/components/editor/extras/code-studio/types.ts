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

export interface CodeStudioData {
  files: CodeFile[]
  folders: FileTreeFolder[]
  openTabs: string[] // IDs dos arquivos abertos em abas
  activeFileId?: string
  mode?: EditorMode
  language: SupportedLanguage
  title?: string
  caption?: string
  
  // Configurações de UI
  readonly?: boolean
  showLineNumbers?: boolean
  fontSize?: number
  theme?: "light" | "dark" | "system"
  isViewMode?: boolean // Se true, mostra apenas código sem console/testes (modo visualização)
  
  // Configurações de execução
  clearOnRun?: boolean
  autoRun?: boolean
  
  // Testes
  testCases?: Record<string, TestCase[]>
  showTests?: boolean
}

export interface ModeConfig {
  id: EditorMode
  label: string
  description: string
  icon: string
  supportedLanguages: SupportedLanguage[]
  showTerminal: boolean
  showTests: boolean
  showPreview: boolean
}

export const MODE_CONFIGS: Record<EditorMode, ModeConfig> = {
  execution: {
    id: "execution",
    label: "Execution",
    description: "Run code and see output in console",
    icon: "Play",
    supportedLanguages: ["javascript", "typescript", "python", "lua", "c", "cpp"],
    showTerminal: true,
    showTests: false,
    showPreview: false,
  },
  test: {
    id: "test",
    label: "Test",
    description: "Write and run test cases",
    icon: "TestTube",
    supportedLanguages: ["javascript", "typescript", "python", "lua", "c", "cpp"],
    showTerminal: false,
    showTests: true,
    showPreview: false,
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
