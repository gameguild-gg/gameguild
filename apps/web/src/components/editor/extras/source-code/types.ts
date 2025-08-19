import type React from "react"
export type LanguageType =
  | "javascript"
  | "typescript"
  | "python"
  | "lua"
  | "c"
  | "cpp"
  | "h"
  | "hpp"
  | "cheader"
  | "cppheader"
  | "xml"
  | "yaml"
  | "json"
  | "html"
  | "css"
  | "markdown"
  | "text"

export type ProgrammingLanguage = "javascript" | "typescript" | "python" | "lua" | "c" | "cpp" | "h" | "hpp"

export interface CodeFile {
  id: string
  name: string
  content: string
  language: LanguageType
  isMain: boolean
  isVisible: boolean
  readOnlyState?: "always" | "never" | "hidden" | null
}

export interface SourceCodeData {
  files?: CodeFile[]
  activeFileId?: string
  readonly?: boolean
  showExecution?: boolean
  isDarkTheme?: boolean
  selectedLanguage?: ProgrammingLanguage
  clearTerminalOnRun?: boolean
  showBasicFileActionsInReadMode?: boolean
  showFilePropertiesInReadMode?: boolean
}

// Update the TerminalProps interface to include the new predicate test type
export interface TerminalProps {
  terminalOutput: {
    id: string
    type: "input" | "output" | "error" | "system"
    content: string | string[]
  }[]
  terminalInput: string
  setTerminalInput: (input: string) => void
  handleTerminalSubmit: () => void
  handleClearTerminal: () => void
  isExecuting: boolean
  handleStopExecution: () => void
  isDarkTheme: boolean
  showTests: boolean
  testCases: Record<
    string,
    {
      type: "custom" | "function" | "console"
      input?: string
      expectedOutput?: string
      args?: any[]
      expectedReturn?: any[]
      predicate?: string
      customCode?: string
      customCodeFirst?: string | Record<ProgrammingLanguage, string>
      customCodeSecond?: string | Record<ProgrammingLanguage, string>
    }[]
  >
  setTestCases: (
    cases: Record<
      string,
      {
        type: "custom" | "function" | "console"
        input?: string
        expectedOutput?: string
        args?: any[]
        expectedReturn?: any[]
        predicate?: string
        customCode?: string
        customCodeFirst?: string | Record<ProgrammingLanguage, string>
        customCodeSecond?: string | Record<ProgrammingLanguage, string>
      }[]
    >,
  ) => void
  testResults: Record<string, { passed: boolean; actual: string; expected: string }[]>
  activeFileId: string
  activeTab: "terminal" | "tests"
  setActiveTab: (tab: "terminal" | "tests") => void
  runTests: (fileId: string) => void
}

export interface CodeEditorProps {
  codeEditorHeight: number
  activeFileLanguage: string
  activeFileContent: string
  isDarkTheme: boolean
  readonly: boolean
  isEditing: boolean
  updateActiveFileContent: (content: string) => void
  handleCodeEditorResize: (e: React.MouseEvent, startY: number) => void
  onEditorMount?: (editor: any, monaco: any) => void
  isAutocompleteEnabled?: boolean
}

export interface LanguageSettingsDialogProps {
  showLanguagesDialog: boolean
  setShowLanguagesDialog: (show: boolean) => void
  allowedLanguages: Record<LanguageType, boolean>
  setAllowedLanguages: React.Dispatch<React.SetStateAction<Record<LanguageType, boolean>>>
  selectedLanguage: ProgrammingLanguage
  setSelectedLanguage: (lang: ProgrammingLanguage) => void
  getLanguageLabel: (lang: string) => string
  updateSourceCode?: (data: any) => void
  isAutocompleteEnabled?: boolean
  setIsAutocompleteEnabled?: (enabled: boolean) => void
}

export interface FileTabsProps {
  files: CodeFile[]
  activeFileId: string
  onFileSelect: (fileId: string) => void
  onFileClose?: (fileId: string) => void
  onFileRename?: (fileId: string, newName: string) => void
  readonly?: boolean
  isDarkTheme?: boolean
}

export interface NewFileDialogProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: (fileName: string, language: LanguageType) => void
  existingFileNames: string[]
}

export interface RenameFileDialogProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: (newName: string) => void
  currentName: string
  existingFileNames: string[]
}

export interface LanguageConfig {
  name: string
  extensions: string[]
  monacoLanguage: string
  highlightLanguage: string
  supportsExecution?: boolean
  defaultTemplate?: string
}
