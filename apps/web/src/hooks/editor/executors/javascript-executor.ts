import type { ProgrammingLanguage } from "../../components/ui/source-code/types"
import type { ExecutionResult, ExecutionContext, LanguageExecutor } from "./types"
import { getFileContent } from "../../components/ui/source-code/utils"

class JavaScriptExecutor implements LanguageExecutor {
  private isExecutionCancelled = false
  public isCompiled = false // Set the isCompiled flag to false
  private globalFunctions: Set<string> = new Set() // Track functions added to global scope
  private debugMode = false // Flag to control debug messages

  execute = async (fileId: string, context: ExecutionContext): Promise<ExecutionResult> => {
    const { files, selectedLanguage, addOutput, setIsExecuting } = context

    this.isExecutionCancelled = false
    setIsExecuting(true)

    try {
      // Clear previously added global functions
      this.clearGlobalFunctions()

      // Find the file to execute
      const visibleFiles = files.filter((file) => file.isVisible)
      const mainFile = visibleFiles.find((file) => file.isMain)
      const activeFile = files.find((file) => file.id === fileId)
      const fileToExecute = mainFile || activeFile

      if (!fileToExecute) {
        addOutput("Error: No file selected for execution.")
        setIsExecuting(false)
        return { success: false, output: ["Error: No file selected for execution."] }
      }

      // If executing a main file that's different from the active file, show a message
      if (mainFile && mainFile.id !== fileId) {
        addOutput(`Executing main file: ${fileToExecute.name}`)
      }

      // Create a virtual file system for imports
      const virtualFileSystem: Record<string, string> = {}
      visibleFiles.forEach((file) => {
        const content = getFileContent(file, selectedLanguage)
        virtualFileSystem[file.name] = content
      })

      // Create a safe execution environment
      const consoleOutput: string[] = []
      const mockConsole = {
        log: (...args: any) => {
          const output = args.map((arg) => String(arg)).join(" ")
          consoleOutput.push(output)
          addOutput(output)
        },
        error: (...args: any) => {
          const output = `Error: ${args.map((arg) => String(arg)).join(" ")}`
          consoleOutput.push(output)
          addOutput(output)
        },
        warn: (...args: any) => {
          const output = `Warning: ${args.map((arg) => String(arg)).join(" ")}`
          consoleOutput.push(output)
          addOutput(output)
        },
      }

      // Custom logger that only shows messages in debug mode
      const debugLog = (message: string) => {
        if (this.debugMode) {
          addOutput(message)
        }
      }

      // Resolve imports and bundle the code (silently)
      debugLog("Resolving imports and bundling files...")
      const bundledCode = this.resolveImports(fileToExecute.name, virtualFileSystem, debugLog)

      // Create a modified version of the bundled code that replaces browser dialogs
      let modifiedCode = bundledCode
        .replace(/alert\s*\(/g, "await customAlert(")
        .replace(/prompt\s*\(/g, "await customPrompt(")
        .replace(/confirm\s*\(/g, "await customConfirm(")

      // Add our custom implementations at the beginning of the code
      modifiedCode = `
       // Custom implementations for browser dialogs
       async function customAlert(message) {
         console.log("ALERT: " + message);
         return new Promise((resolve) => {
           window.__awaitingAlertAck = true;
           window.__alertMessage = message;
           
           window.alertCallback = () => {
             window.__awaitingAlertAck = false;
             window.__alertMessage = null;
             resolve();
           };
         });
       }

       async function customPrompt(message, defaultValue = "") {
         console.log("PROMPT: " + message + (defaultValue ? " [default: " + defaultValue + "]" : ""));
         return new Promise((resolve) => {
           window.__awaitingPromptInput = true;
           window.__promptMessage = message;
           
           window.promptCallback = (value) => {
             window.__awaitingPromptInput = false;
             window.__promptMessage = null;
             resolve(value || defaultValue);
           };
         });
       }

       async function customConfirm(message) {
         console.log("CONFIRM: " + message);
         return new Promise((resolve) => {
           window.__awaitingConfirmInput = true;
           window.__confirmMessage = message;
           
           window.confirmCallback = (value) => {
             window.__awaitingConfirmInput = false;
             window.__confirmMessage = null;
             resolve(value === "0" || value.toLowerCase() === "y" || value.toLowerCase() === "yes" || value.toLowerCase() === "true");
           };
         });
       }

       // Main execution
       try {
         ${modifiedCode}
       } catch (error) {
         console.error("Runtime error: " + error.message);
       }
     `

      // Check if execution was cancelled
      if (this.isExecutionCancelled) {
        addOutput("Execution cancelled.")
        setIsExecuting(false)
        return { success: false, output: ["Execution cancelled."] }
      }

      // Execute the code in a safe context
      const AsyncFunction = Object.getPrototypeOf(async () => {}).constructor
      const executeCode = new AsyncFunction("console", "window", modifiedCode)
      await executeCode(mockConsole, window)

      setIsExecuting(false)
      return { success: true, output: consoleOutput }
    } catch (error) {
      console.error("JavaScript execution error:", error)
      const errorMessage = error instanceof Error ? error.message : String(error)
      addOutput(`Error: ${errorMessage}`)
      setIsExecuting(false)
      return { success: false, output: [`Error: ${errorMessage}`] }
    }
  }

  private clearGlobalFunctions(): void {
    // Remove all previously added global functions
    this.globalFunctions.forEach((functionName) => {
      if (window[functionName]) {
        delete window[functionName]
      }
    })
    this.globalFunctions.clear()
  }

  private addToGlobalScope(functionName: string): void {
    // Track functions added to global scope
    this.globalFunctions.add(functionName)
  }

  private isLineCommented(content: string, matchIndex: number): boolean {
    // Find the start of the line containing the match
    const beforeMatch = content.substring(0, matchIndex)
    const lastNewlineIndex = beforeMatch.lastIndexOf("\n")
    const lineStart = lastNewlineIndex === -1 ? 0 : lastNewlineIndex + 1

    // Get the content from line start to the match
    const lineBeforeMatch = content.substring(lineStart, matchIndex)

    // Check if the line starts with // (ignoring whitespace)
    return /^\s*\/\//.test(lineBeforeMatch)
  }

  private extractExports(content: string): {
    named: Set<string>
    hasDefaultExport: boolean
    defaultExportName: string | null
  } {
    const namedExports = new Set<string>()
    let hasDefaultExport = false
    let defaultExportName: string | null = null

    // Remove comments to avoid false positives
    const contentWithoutComments = content.replace(/\/\/.*$/gm, "").replace(/\/\*[\s\S]*?\*\//g, "")

    // Pattern 1: export { name1, name2 }
    const namedExportRegex = /export\s*{\s*([^}]+)\s*}/g
    let match
    while ((match = namedExportRegex.exec(contentWithoutComments)) !== null) {
      const exportList = match[1]
        .split(",")
        .map((name) => name.trim())
        .filter((name) => name.length > 0)
      exportList.forEach((name) => namedExports.add(name))
    }

    // Pattern 2: export function functionName()
    const exportFunctionRegex = /export\s+function\s+(\w+)/g
    while ((match = exportFunctionRegex.exec(contentWithoutComments)) !== null) {
      namedExports.add(match[1])
    }

    // Pattern 3: export const variableName =
    const exportConstRegex = /export\s+const\s+(\w+)/g
    while ((match = exportConstRegex.exec(contentWithoutComments)) !== null) {
      namedExports.add(match[1])
    }

    // Pattern 4: export let variableName =
    const exportLetRegex = /export\s+let\s+(\w+)/g
    while ((match = exportLetRegex.exec(contentWithoutComments)) !== null) {
      namedExports.add(match[1])
    }

    // Pattern 5: export var variableName =
    const exportVarRegex = /export\s+var\s+(\w+)/g
    while ((match = exportVarRegex.exec(contentWithoutComments)) !== null) {
      namedExports.add(match[1])
    }

    // Pattern 6: export default expression
    const exportDefaultRegex = /export\s+default\s+(\w+)/
    match = exportDefaultRegex.exec(contentWithoutComments)
    if (match) {
      hasDefaultExport = true
      defaultExportName = match[1]
    }

    // Pattern 7: export default function name() or export default class name
    const exportDefaultFunctionRegex = /export\s+default\s+(function|class)\s+(\w+)/
    match = exportDefaultFunctionRegex.exec(contentWithoutComments)
    if (match) {
      hasDefaultExport = true
      defaultExportName = match[2]
    }

    // Pattern 8: export default anonymous function or object
    const exportDefaultAnonRegex = /export\s+default\s+(function\s*\(|class\s*{|\{|\[)/
    if (exportDefaultAnonRegex.test(contentWithoutComments)) {
      hasDefaultExport = true
      defaultExportName = null // Anonymous export
    }

    return { named: namedExports, hasDefaultExport, defaultExportName }
  }

  private resolveImports(entryFile: string, vfs: Record<string, string>, debugLog: (msg: string) => void): string {
    const visited = new Set<string>()
    const importRegex = /import\s+(?:(\w+)|{([^}]+)}|\*\s+as\s+(\w+))?\s*from\s*['"](.+?)['"];?/g

    const resolveFile = (fileName: string): string => {
      if (visited.has(fileName)) {
        debugLog(`Circular import detected: ${fileName}`)
        return ""
      }
      visited.add(fileName)

      const content = vfs[fileName]
      if (!content) {
        debugLog(`Warning: File not found: ${fileName}`)
        return ""
      }

      let result = ""
      const imports = Array.from(content.matchAll(importRegex))

      // Filter out commented imports
      const activeImports = imports.filter((match) => {
        const isCommented = this.isLineCommented(content, match.index || 0)
        if (isCommented) {
          debugLog(`Skipping commented import: ${match[0].trim()}`)
        }
        return !isCommented
      })

      // Process active imports first
      for (const match of activeImports) {
        const [fullMatch, defaultImport, namedImports, namespaceImport, importPath] = match
        const resolvedPath = this.resolveRelativePath(fileName, importPath, vfs)

        if (resolvedPath) {
          debugLog(`Importing: ${importPath} -> ${resolvedPath}`)

          // Get the imported file content
          const importedContent = resolveFile(resolvedPath)
          const importedFileContent = vfs[resolvedPath]

          // Extract exports from the imported file
          const {
            named: availableExports,
            hasDefaultExport,
            defaultExportName,
          } = this.extractExports(importedFileContent)

          debugLog(
            `Available exports from ${resolvedPath}: [${Array.from(availableExports).join(", ")}]${
              hasDefaultExport ? ` with default export${defaultExportName ? ` (${defaultExportName})` : ""}` : ""
            }`,
          )

          // Handle default import
          if (defaultImport) {
            if (!hasDefaultExport) {
              debugLog(`Error: No default export found in ${resolvedPath}`)
              continue
            }

            this.addToGlobalScope(defaultImport)

            // If we know the name of the default export, use it directly
            if (defaultExportName) {
              result += `
// === Default import from ${resolvedPath} ===
(function() {
${importedContent}

// Export default to global scope
if (typeof ${defaultExportName} !== 'undefined') { 
  window.${defaultImport} = ${defaultExportName}; 
}
})();
`
            } else {
              // For anonymous default exports, we need to extract it differently
              result += `
// === Default import from ${resolvedPath} ===
(function() {
${importedContent}

// For anonymous default export, we need to capture it
// This is a simplified approach - in a real bundler this would be more sophisticated
window.${defaultImport} = (function() {
  // The default export should be the last expression in the file
  var exports = {};
  ${importedContent.replace(/export\s+default\s+/, "exports.default = ")}
  return exports.default;
})();
})();
`
            }
          }
          // Handle named imports
          else if (namedImports) {
            const requestedImports = namedImports.split(",").map((name) => name.trim())
            debugLog(`Requested imports: {${requestedImports.join(", ")}} from ${resolvedPath}`)

            // Check if all requested imports are available
            const unavailableImports = requestedImports.filter((name) => !availableExports.has(name))
            if (unavailableImports.length > 0) {
              debugLog(
                `Error: The following exports are not available in ${resolvedPath}: ${unavailableImports.join(", ")}`,
              )
              debugLog(`Available exports: ${Array.from(availableExports).join(", ") || "none"}`)
              continue // Skip this import
            }

            // Track the functions we're adding to global scope
            requestedImports.forEach((name) => this.addToGlobalScope(name))

            // Create a wrapper that exposes only the named exports
            result += `
// === Named imports from ${resolvedPath} ===
(function() {
${importedContent}

// Export only the explicitly exported and requested functions to global scope
${requestedImports.map((name) => `if (typeof ${name} !== 'undefined') { window.${name} = ${name}; }`).join("\n")}
})();
`
          }
          // Handle namespace imports (import * as name)
          else if (namespaceImport) {
            this.addToGlobalScope(namespaceImport)

            // Create a namespace object with all exports
            result += `
// === Namespace import from ${resolvedPath} ===
(function() {
${importedContent}

// Create namespace object
window.${namespaceImport} = {};

// Add all named exports to namespace
${Array.from(availableExports)
  .map((name) => `if (typeof ${name} !== 'undefined') { window.${namespaceImport}.${name} = ${name}; }`)
  .join("\n")}

// Add default export to namespace if available
${
  hasDefaultExport && defaultExportName
    ? `if (typeof ${defaultExportName} !== 'undefined') { window.${namespaceImport}.default = ${defaultExportName}; }`
    : ""
}
})();
`
          } else {
            // Regular import - just include the file
            result += importedContent + "\n"
          }
        }
      }

      // Remove ALL import statements and export statements, then add the file content
      const contentWithoutImportsAndExports = content
        .replace(importRegex, "")
        .replace(/export\s*{\s*[^}]+\s*};?/g, "") // Remove export { ... }
        .replace(/export\s+(function|const|let|var|class)\s+/g, "$1 ") // Remove export keyword from declarations
        .replace(/export\s+default\s+/g, "") // Remove export default

      result += contentWithoutImportsAndExports + "\n"

      return result
    }

    debugLog(`Starting bundle from: ${entryFile}`)
    return resolveFile(entryFile)
  }

  private resolveRelativePath(currentFile: string, importPath: string, vfs: Record<string, string>): string | null {
    // Handle relative imports starting with './'
    if (importPath.startsWith("./")) {
      const targetFile = importPath.substring(2)

      // Try with .js extension if not present
      if (vfs[targetFile]) {
        return targetFile
      }
      if (!targetFile.includes(".") && vfs[targetFile + ".js"]) {
        return targetFile + ".js"
      }
    }

    // Handle direct file names
    if (vfs[importPath]) {
      return importPath
    }

    // Try with .js extension
    if (!importPath.includes(".") && vfs[importPath + ".js"]) {
      return importPath + ".js"
    }

    return null
  }

  stop = () => {
    this.isExecutionCancelled = true
  }

  getFileExtension = (): string => {
    return "js"
  }

  getSupportedLanguages = (): ProgrammingLanguage[] => {
    return ["javascript"]
  }

  handleCommand = (command: string, context: ExecutionContext): boolean => {
    const { addOutput } = context

    // Try to evaluate JavaScript directly
    if (!command.includes("console.log")) {
      try {
        const result = eval(command)
        addOutput(typeof result === "undefined" ? "undefined" : String(result))
        return true
      } catch (error) {
        addOutput(`Error: ${error instanceof Error ? error.message : String(error)}`)
        return true
      }
    }

    return false
  }
}

// Export as named export
export const javascriptExecutor = new JavaScriptExecutor()
