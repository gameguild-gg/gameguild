import { getExecutor } from "../../executors/executor-factory"
import type { TestRunnerOptions } from "../types"

export async function runSimpleTestsJavaScript(options: TestRunnerOptions) {
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

  // Store the current test index in a global variable so the terminal component can access it
  window.__currentTestIndex = -1

  for (let i = 0; i < fileCases.length; i++) {
    // Update the current test index for each test
    window.__currentTestIndex = i

    const testCase = fileCases[i]

    // Get the appropriate executor for the selected language
    const executor = getExecutor(selectedLanguage)

    // Create a local array to capture output for this specific test
    let testOutputLines: string[] = []

    // Get the file content
    const userCode = file.languageContent?.[selectedLanguage] || file.content || ""

    // Create a test harness that sets up mock console input
    const testInputValue = testCase.args && testCase.args.length > 0 ? testCase.args[0] : undefined
    const testHarnessCode = `
// System Test Code:
// Set up mock console input
let __inputValue = ${JSON.stringify(testInputValue)};
let __originalPrompt = prompt;

// Override prompt to return test input
window.prompt = function(message) {
  console.log(message || "");
  const result = __inputValue;
  return result;
};

// User code:
${userCode}

// Restore original prompt
window.prompt = __originalPrompt;
`

    // Create a modified file with the test harness
    const modifiedFile = {
      ...file,
      content: testHarnessCode,
      languageContent: {
        ...file.languageContent,
        [selectedLanguage]: testHarnessCode,
      },
    }

    // Create a file collection with the modified file
    const modifiedFiles = files.map((f) => (f.id === fileId ? modifiedFile : f))

    // Create execution context with captured output
    const context = {
      files: modifiedFiles,
      selectedLanguage,
      addOutput: (output: string | string[]) => {
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

    // Filter out prompt messages and system messages from the output
    const filteredOutputLines = testOutputLines.filter((line) => {
      const lineStr = typeof line === "string" ? line : String(line)
      return (
        !lineStr.startsWith("PROMPT:") &&
        !lineStr.startsWith("$ ") &&
        !lineStr.startsWith("[Test") &&
        !lineStr.startsWith("--- ")
      )
    })

    // Get the captured output after filtering
    const actualOutput = filteredOutputLines.join("\n")

    // Normalize both outputs for comparison (trim whitespace, etc.)
    const normalizedActual = normalizeOutput(actualOutput)
    const normalizedExpected = normalizeOutput(testCase.expectedOutput || "")

    // Compare output with expected output
    const passed = normalizedActual === normalizedExpected

    // Add detailed test result information to terminal output
    addOutput(`[Test ${i + 1}] ${passed ? "✓ Passed" : "✗ Failed"}`)
    if (!passed) {
      addOutput(`  Expected: ${JSON.stringify(normalizedExpected)}`)
      addOutput(`  Actual: ${JSON.stringify(normalizedActual)}`)
    }

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

    // Reset prompt state after each test
    window.__awaitingPromptInput = false
    window.promptCallback = null
  }

  // Reset the current test index when all tests are complete
  window.__currentTestIndex = -1
}
