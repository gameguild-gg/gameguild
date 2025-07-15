import type { ProgrammingLanguage } from "@/components/editor/ui/source-code/types"
import type { ExecutionResult, ExecutionContext, LanguageExecutor } from "./types"
import { useCode } from "@/components/code/use-code"

class PythonExecutor implements LanguageExecutor {
  private isExecutionCancelled = false
  public isCompiled = false // Set the isCompiled flag to false
  private codeRunner: ReturnType<typeof useCode> | null = null

  constructor() {
    // Singleton para o hook useCode
    if (!PythonExecutor.instance) {
      PythonExecutor.instance = this
      this.codeRunner = useCode()
    } else {
      return PythonExecutor.instance
    }
  }
  static instance: PythonExecutor | null = null

  execute = async (fileId: string, context: ExecutionContext): Promise<ExecutionResult> => {
    const { addOutput, setIsExecuting, files } = context
    this.isExecutionCancelled = false
    setIsExecuting(true)

    // Obtém o código do arquivo
    let code = ""
    if (files) {
      if (Array.isArray(files)) {
        // Se for array, busca pelo id
        const found = files.find((f: any) => f.id === fileId)
        if (found && typeof found.content === "string") {
          code = found.content
        }
      } else if (fileId in files) {
        // Se for objeto
        code = (files as Record<string, string>)[fileId]
      }
    }

    if (!this.codeRunner) {
      this.codeRunner = useCode()
    }

    // Executa o código Python usando o hook useCode
    const result = await this.codeRunner.compileAndRun({
      language: "python",
      data: { [fileId]: code },
      stdin: undefined,
    })

    addOutput([result.output])
    setIsExecuting(false)
    return {
      success: result.success,
      output: [result.output],
    }
  }

  stop = () => {
    this.isExecutionCancelled = true
    if (this.codeRunner) {
      this.codeRunner.abort()
    }
  }

  getFileExtension = (): string => {
    return "py"
  }

  getSupportedLanguages = (): ProgrammingLanguage[] => {
    return ["python"]
  }
}

export const pythonExecutor = new PythonExecutor()
