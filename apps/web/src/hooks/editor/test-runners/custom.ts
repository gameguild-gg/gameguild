import { getExecutor } from "../executors/executor-factory"
import type { TestRunnerOptions } from "./types"
import type { ProgrammingLanguage } from "@/components/editor/ui/source-code/types"

export async function runCustomTests(options: TestRunnerOptions) {
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

    // Get the appropriate executor for the selected language
    const executor = getExecutor(selectedLanguage)

    // Create a local array to capture output for this specific test
    let testOutputLines: string[] = []

    // Get the file content
    const userCode = file.languageContent?.[selectedLanguage] || file.content || ""

    // Get the custom code sections (before and after) for the current language
    let customCodeFirst = ""
    if (typeof testCase.customCodeFirst === "string") {
      customCodeFirst = testCase.customCodeFirst
    } else if (testCase.customCodeFirst && typeof testCase.customCodeFirst === "object") {
      customCodeFirst = testCase.customCodeFirst[selectedLanguage as ProgrammingLanguage] || ""
    } else if (typeof testCase.customCode === "string") {
      // Backward compatibility
      customCodeFirst = testCase.customCode
    }

    let customCodeSecond = ""
    if (typeof testCase.customCodeSecond === "string") {
      customCodeSecond = testCase.customCodeSecond
    } else if (testCase.customCodeSecond && typeof testCase.customCodeSecond === "object") {
      customCodeSecond = testCase.customCodeSecond[selectedLanguage as ProgrammingLanguage] || ""
    }

    // Get the test-specific code sections
    let testSpecificFirst = ""
    if (typeof testCase.testSpecificFirst === "string") {
      testSpecificFirst = testCase.testSpecificFirst
    } else if (testCase.testSpecificFirst && typeof testCase.testSpecificFirst === "object") {
      testSpecificFirst = testCase.testSpecificFirst[selectedLanguage as ProgrammingLanguage] || ""
    }

    let testSpecificSecond = ""
    if (typeof testCase.testSpecificSecond === "string") {
      testSpecificSecond = testCase.testSpecificSecond
    } else if (testCase.testSpecificSecond && typeof testCase.testSpecificSecond === "object") {
      testSpecificSecond = testCase.testSpecificSecond[selectedLanguage as ProgrammingLanguage] || ""
    }

    // Default second code section if empty
    if (!customCodeSecond) {
      customCodeSecond = getDefaultSecondCode(selectedLanguage)
    }

    // Create a test harness with both custom code sections and test-specific code
    const testHarnessCode = `
// System Test Code (Before student code - Hidden from student):
${customCodeFirst}

// Test-specific setup code:
${testSpecificFirst}

// User code:
${userCode}

// System Test Code (After student code - Hidden from student):
${customCodeSecond}

// Test-specific validation code:
${testSpecificSecond}
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

    // Get the captured output
    const actualOutput = testOutputLines.join("\n")

    // Check if the output contains "Test result: PASSED"
    const passed = actualOutput.includes("Test result: PASSED")

    // Update test results
    setTestResults((prev) => {
      const fileResults = [...(prev[fileId] || [])]
      fileResults[i] = {
        passed,
        actual: actualOutput,
        expected: testCase.expectedOutput || "Test result: PASSED",
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

// Helper function to get default second code based on language
function getDefaultSecondCode(language: string): string {
  switch (language) {
    case "javascript":
      return `
// Default test validation code
const expectedReturn = null; // Replace with your expected value
const actualReturn = null;   // Capture your function's return value

console.log("Test result: " + (expectedReturn === actualReturn ? "PASSED" : "FAILED"));
if (expectedReturn !== actualReturn) {
  console.log("Expected: " + JSON.stringify(expectedReturn));
  console.log("Actual: " + JSON.stringify(actualReturn));
}
`
    case "typescript":
      return `
// Default test validation code
const expectedReturn: any = null; // Replace with your expected value
const actualReturn: any = null;   // Capture your function's return value

console.log("Test result: " + (expectedReturn === actualReturn ? "PASSED" : "FAILED"));
if (expectedReturn !== actualReturn) {
  console.log("Expected: " + JSON.stringify(expectedReturn));
  console.log("Actual: " + JSON.stringify(actualReturn));
}
`
    case "python":
      return `
# Default test validation code
expected_return = None  # Replace with your expected value
actual_return = None    # Capture your function's return value

import json
print("Test result: " + ("PASSED" if expected_return == actual_return else "FAILED"))
if expected_return != actual_return:
    print("Expected: " + json.dumps(expected_return))
    print("Actual: " + json.dumps(actual_return))
`
    case "lua":
      return `
-- Default test validation code
local expected_return = nil  -- Replace with your expected value
local actual_return = nil    -- Capture your function's return value

print("Test result: " .. (expected_return == actual_return and "PASSED" or "FAILED"))
if expected_return ~= actual_return then
    print("Expected: " .. tostring(expected_return))
    print("Actual: " .. tostring(actual_return))
end
`
    case "c":
    case "cpp":
      return `
// Default test validation code
// Replace with your expected value and actual return capture
int expected_return = 0;
int actual_return = 0;

printf("Test result: %s\\n", expected_return == actual_return ? "PASSED" : "FAILED");
if (expected_return != actual_return) {
    printf("Expected: %d\\n", expected_return);
    printf("Actual: %d\\n", actual_return);
}
`
    case "h":
    case "hpp":
      return `
// Default test validation code
// Replace with your expected value and actual return capture
int expected_return = 0;
int actual_return = 0;

printf("Test result: %s\\n", expected_return == actual_return ? "PASSED" : "FAILED");
if (expected_return != actual_return) {
    printf("Expected: %d\\n", expected_return);
    printf("Actual: %d\\n", actual_return);
}
`
    default:
      return `
// Default test validation code
// Replace with appropriate code for your language
// Print "Test result: PASSED" if the test passes
// Print "Test result: FAILED" if the test fails
`
  }
}
