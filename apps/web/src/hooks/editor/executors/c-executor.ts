import type { ProgrammingLanguage } from "@/components/editor/extras/source-code/types"
import type { ExecutionContext, ExecutionResult, LanguageExecutor } from "./types"

class CExecutor implements LanguageExecutor {
  private isExecutionCancelled = false
  public isCompiled = true // Set the isCompiled flag to true for C

  execute = async (fileId: string, context: ExecutionContext): Promise<ExecutionResult> => {
    if (!context) {
      console.error(`C execution error: context is undefined`)
      return {
        success: false,
        output: [`Error: Execution context is undefined`],
      }
    }

    const { addOutput, setIsExecuting } = context

    // Set executing state
    setIsExecuting(true)

    // Provide a clear message about the implementation status
    addOutput([
      `C compilation and execution is not fully implemented yet.`,
      `This is a placeholder that will be replaced with actual C compilation in the future.`,
      `For now, here's a simulated output.`,
    ])

    // Simulate compilation and execution
    await new Promise((resolve) => setTimeout(resolve, 1000))

    addOutput([
      "Compilation successful",
      "Program executed successfully.",
      "This is a simulated output from a C program.",
    ])

    // Reset executing state
    setIsExecuting(false)

    return {
      success: true,
      output: ["Program executed successfully.", "This is a simulated output from a C program."],
    }
  }

  stop = () => {
    // Set the cancellation flag
    this.isExecutionCancelled = true
  }

  getFileExtension = (): string => {
    return "c"
  }

  getSupportedLanguages = (): ProgrammingLanguage[] => {
    return ["c"]
  }
}

export const cExecutor = new CExecutor()
