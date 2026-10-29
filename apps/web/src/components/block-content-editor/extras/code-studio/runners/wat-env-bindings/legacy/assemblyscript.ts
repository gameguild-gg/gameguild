/**
 * AssemblyScript-specific bindings
 */
export function createAssemblyScriptBindings(
  stdout: (text: string) => void,
  stderr: (text: string) => void
) {
  return {
    // AssemblyScript runtime hooks
    abort: (message: string | number, fileName: string | number, line: number, column: number) => {
      stderr(`AssemblyScript abort: ${message} at ${fileName}:${line}:${column}`)
      throw new Error(`Abort: ${message}`)
    },

    trace: (message: string | number, n: number, ...args: number[]) => {
      stdout(`[TRACE] ${message} n=${n} args=${args.join(', ')}`)
    },

    seed: () => Date.now(),
  }
}
