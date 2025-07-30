import type { CodeFile, ProgrammingLanguage } from "@/components/editor/ui/source-code/types"

export interface TestResult {
  passed: boolean;
  actual: string;
  expected: string;
}

export type TestType = 'simple' | 'inout' | 'predicate';

export interface TestRunnerOptions {
  fileId: string
  file: CodeFile
  files: CodeFile[]
  selectedLanguage: ProgrammingLanguage
  addOutput: (output: string | string[]) => void
  clearTerminal: () => void
  setIsExecuting: (isExecuting: boolean) => void
  setTestResults: (results: Record<string, { passed: boolean; actual: string; expected: string }[]>) => void
  normalizeOutput: (output: string) => string
}

export type TestRunner = (options: TestRunnerOptions) => Promise<void>;
