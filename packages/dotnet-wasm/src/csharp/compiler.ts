import type { MonoRuntime } from '../types'

export interface CompilationResult {
  success: boolean
  assembly?: Uint8Array
  errors: string[]
  warnings: string[]
}

export interface CompilerOptions {
  assemblyName?: string
  optimize?: boolean
  allowUnsafe?: boolean
  references?: string[]
}

export class Compiler {
  private runtime: MonoRuntime

  constructor(runtime: MonoRuntime) {
    this.runtime = runtime
  }

  /**
   * Compile C# source code to IL assembly
   */
  async compile(
    sourceCode: string,
    options: CompilerOptions = {}
  ): Promise<CompilationResult> {
    const assemblyName = options.assemblyName || 'UserProgram'
    const optimize = options.optimize ?? false
    const allowUnsafe = options.allowUnsafe ?? false

    try {
      // Create a unique temporary path for this compilation
      const tempId = `compile_${Date.now()}_${Math.random().toString(36).substring(7)}`
      const sourcePath = `/tmp/${tempId}.cs`
      const outputPath = `/tmp/${tempId}.dll`

      // Write source code to virtual filesystem
      const fs = this.runtime.Module.FS
      
      // Ensure /tmp exists
      try {
        fs.mkdir('/tmp')
      } catch (e) {
        // Directory might already exist
      }

      fs.writeFile(sourcePath, sourceCode)

      // Prepare compilation arguments
      const args = [
        'RoslynWrapper.dll',
        'compile',
        '--source', sourcePath,
        '--output', outputPath,
        '--assembly-name', assemblyName,
      ]

      if (optimize) {
        args.push('--optimize')
      }

      if (allowUnsafe) {
        args.push('--allow-unsafe')
      }

      // Add default references
      const defaultReferences = [
        'System.Runtime',
        'System.Console',
        'System.Collections',
        'System.Linq',
        ...(options.references || [])
      ]

      for (const ref of defaultReferences) {
        args.push('--reference', ref)
      }

      // Capture output
      let stdout = ''
      let stderr = ''
      const originalPrint = this.runtime.Module.print
      const originalPrintErr = this.runtime.Module.printErr

      this.runtime.Module.print = (text: string) => {
        stdout += text + '\n'
      }
      this.runtime.Module.printErr = (text: string) => {
        stderr += text + '\n'
      }

      try {
        // Call RoslynWrapper to compile
        const exitCode = this.runtime.MONO.mono_call_assembly_entry_point(
          'RoslynWrapper',
          args,
          'Main'
        )

        // Read compilation result
        let assembly: Uint8Array | undefined
        const errors: string[] = []
        const warnings: string[] = []

        // Parse output for errors and warnings
        const lines = (stdout + stderr).split('\n')
        for (const line of lines) {
          if (line.includes('error CS')) {
            errors.push(line)
          } else if (line.includes('warning CS')) {
            warnings.push(line)
          }
        }

        // If compilation succeeded, read the assembly
        if (exitCode === 0) {
          try {
            const assemblyData = fs.readFile(outputPath) as Uint8Array
            assembly = new Uint8Array(assemblyData)
          } catch (e) {
            errors.push(`Failed to read compiled assembly: ${e}`)
          }
        }

        // Cleanup temporary files
        try {
          fs.unlink(sourcePath)
          if (exitCode === 0) {
            fs.unlink(outputPath)
          }
        } catch (e) {
          // Ignore cleanup errors
        }

        return {
          success: exitCode === 0 && errors.length === 0,
          assembly,
          errors,
          warnings,
        }
      } finally {
        // Restore original output handlers
        this.runtime.Module.print = originalPrint
        this.runtime.Module.printErr = originalPrintErr
      }
    } catch (error) {
      return {
        success: false,
        errors: [
          error instanceof Error
            ? error.message
            : 'Unknown compilation error'
        ],
        warnings: [],
      }
    }
  }

  /**
   * Validate C# syntax without full compilation
   */
  async validateSyntax(sourceCode: string): Promise<{
    isValid: boolean
    errors: string[]
  }> {
    try {
      const result = await this.compile(sourceCode, {
        assemblyName: 'SyntaxCheck',
      })

      return {
        isValid: result.success,
        errors: result.errors,
      }
    } catch (error) {
      return {
        isValid: false,
        errors: [
          error instanceof Error
            ? error.message
            : 'Syntax validation failed'
        ],
      }
    }
  }
}
