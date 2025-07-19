import type { CodeFile, ProgrammingLanguage } from "../../components/ui/source-code/types"

export interface TestCase {
  type: "simple" | "inout" | "predicate"
  input?: string
  expectedOutput?: string
  args?: any[] // For inout tests: array of arguments to pass to the solution function
  expectedReturn?: any[] // Expected return value(s) from the solution function
  predicate?: string
}

export interface TestResult {
  passed: boolean
  actual: string
  expected: string
}

export type TestType = "simple" | "inout" | "predicate"

export interface TestRunnerOptions {
  fileId: string
  file: CodeFile
  fileCases: TestCase[]
  files: CodeFile[]
  selectedLanguage: ProgrammingLanguage
  addOutput: (output: string | string[]) => void
  clearTerminal: () => void
  setIsExecuting: (isExecuting: boolean) => void
  setTestResults: (results: Record<string, { passed: boolean; actual: string; expected: string }[]>) => void
  normalizeOutput: (output: string) => string
}

export type TestRunner = (options: TestRunnerOptions) => Promise<void>
