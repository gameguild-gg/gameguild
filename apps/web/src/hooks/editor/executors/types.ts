import type { CodeFile, ProgrammingLanguage } from "@/components/block-content-editor/extras/source-code/types"
// Replace the above line with the correct import or define CodeFile here if missing:

// Add global type definitions for the prompt and confirm flags

// Note: Window interface is extended elsewhere with stricter types
// These callback properties support null/undefined at runtime even if declared as required

export interface ExecutionContext {
  files: CodeFile[]
  selectedLanguage: ProgrammingLanguage
  addOutput: (output: string | string[]) => void
  setIsExecuting: (isExecuting: boolean) => void
  clearTerminal?: () => void
}

export interface ExecutionResult {
  success: boolean
  output: string[]
}

export interface LanguageExecutor {
  isCompiled?: boolean
  execute: (fileId: string, context: ExecutionContext) => Promise<ExecutionResult>
  stop: () => void
  getFileExtension?: () => string
  getSupportedLanguages: () => ProgrammingLanguage[]
  handleCommand?: (command: string, context: ExecutionContext) => boolean
}


export type Executor = LanguageExecutor