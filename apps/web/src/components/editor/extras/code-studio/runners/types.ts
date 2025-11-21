export interface RunnerResult {
  stdout: string
  stderr: string
  exitCode: number
  executionTime: number
}

export interface CodeRunner {
  execute(code: string, stdin?: string): Promise<RunnerResult>
  interrupt(): Promise<void>
  dispose(): void
}

export interface RunnerOptions {
  timeout?: number // ms
  memoryLimit?: number // bytes
}
