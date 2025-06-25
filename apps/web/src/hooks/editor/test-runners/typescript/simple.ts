import type { TestRunner, TestRunnerOptions } from "../types"
import { getExecutor } from "../../executors/executor-factory"
import type { ExecutionContext } from "../../executors/types"

/**
 * Run simple output-based tests for TypeScript
 */
export const runSimpleTestsTypeScript: TestRunner = async (options: TestRunnerOptions) => {
  const {
    fileId,
    file,
    fileCases,
    files,
    selectedLanguage,
    addOutput,
    clearTerminal,
    setIsExecuting,
    setTestResults,
    normalizeOutput,
  } = options

  for (let i = 0; i < fileCases.length; i++) {
    const testCase = fileCases[i]

    // Get the appropriate executor for TypeScript
    const executor = getExecutor(selectedLanguage)

    // Create a local array to capture output for this specific test
    let testOutputLines: string[] = []

    // Create execution context with captured output
    const context: ExecutionContext = {
      files,
      selectedLanguage,
      addOutput: (output) => {
        // Capture the output for test comparison
        if (Array.isArray(output)) {
          testOutputLines = [...testOutputLines, ...output]
        } else {
          testOutputLines = [...testOutputLines, output]
        }

        // Also show in the terminal
        addOutput(output)
      },
      clearTerminal,
      setIsExecuting,
    }

    addOutput(`[Test ${i + 1}] Running...`)

    try {
      // Execute the code with proper error handling
      await executor.execute(fileId, context)
    } catch (error) {
      console.error(`Test execution error:`, error)
      const errorMessage = error instanceof Error ? error.message : String(error)
      addOutput(`Error: ${errorMessage}`)
      testOutputLines.push(`Error: ${errorMessage}`)
    }

    // Get the captured output
    const actualOutput = testOutputLines.join("\n")

    // Normalize both outputs for comparison (trim whitespace, etc.)
    const normalizedActual = normalizeOutput(actualOutput)
    const normalizedExpected = normalizeOutput(testCase.expectedOutput || "")

    // Compare output with expected output
    const passed = normalizedActual === normalizedExpected

    // Update test results
    setTestResults((prev) => {
      const fileResults = [...(prev[fileId] || [])]
      fileResults[i] = {
        passed,
        actual: actualOutput,
        expected: testCase.expectedOutput || "",
      }
      return {
        ...prev,
        [fileId]: fileResults,
      }
    })

    // Add test result to terminal output
    addOutput(`[Test ${i + 1}] ${passed ? "✓ Passed" : "✗ Failed"}`)
  }
}
