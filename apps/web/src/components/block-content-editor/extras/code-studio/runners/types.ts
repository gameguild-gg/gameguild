export interface RunnerResult {
  stdout: string
  stderr: string
  exitCode: number
  executionTime: number
}

export interface FileMap {
  [path: string]: string
}

export interface CodeRunner {
  execute(code: string, stdin?: string): Promise<RunnerResult>
  executeWithFiles?(entryPoint: string, files: FileMap, stdin?: string): Promise<RunnerResult>
  interrupt(): Promise<void>
  dispose(): void
}

export interface RunnerOptions {
  timeout?: number // ms
  memoryLimit?: number // bytes
  onRequestInput?: (prompt?: string, currentOutput?: string) => Promise<string> // Callback for interactive input
  onProgress?: (message: string) => void // Callback para feedback de progresso
}
