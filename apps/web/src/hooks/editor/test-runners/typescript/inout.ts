import type { TestRunner, TestRunnerOptions } from "../types"
import { getExecutor } from "../../executors/executor-factory"
import type { ExecutionContext } from "../../executors/types"

/**
 * Run In/Out tests for TypeScript
 */
export const runInOutTestsTypeScript: TestRunner = async (options: TestRunnerOptions) => {
  const { fileId, file, fileCases, files, selectedLanguage, addOutput, clearTerminal, setIsExecuting, setTestResults } =
    options

  // Create a modified file with the TypeScript test harness prepended
  const testHarnessCode = `
// System Test Code:

// Define test case interface
interface TestCase {
  args: any[];
  expectedReturn: any[];
}

// Tests based on user-defined test cases
const functionCodingTests = {
  publicTests: [
`

  // Build the test cases array manually to avoid JSON serialization issues
  let testCasesCode = ""
  fileCases.forEach((test, index) => {
    const isLast = index === fileCases.length - 1
    testCasesCode += `    {
      args: ${JSON.stringify(test.args || [])},
      expectedReturn: ${JSON.stringify(test.expectedReturn || [])}
    }${isLast ? "" : ","}\n`
  })

  const testHarnessCode2 = `
  ]
};

// Deep comparison function
function deepEqual(a, b) {
  return JSON.stringify(a) === JSON.stringify(b);
}

// Execute tests with array inputs and outputs
function runFunctionTests(solution, tests) {
  tests.forEach((test, index) => {
    try {
      // Ensure args is always an array
      const args = Array.isArray(test.args) ? test.args : [];
      // Pass the entire array as a single parameter
      const result = solution(args);
      const passed = deepEqual([result], test.expectedReturn);
      
      // More compact, English output format
      const resultText = "Test #" + (index + 1) + ": " + (passed ? "✓ PASS" : "✗ FAIL") + 
                         " | Input: " + JSON.stringify(args) + 
                         " | Expected: " + JSON.stringify(test.expectedReturn) + 
                         " | Got: " + JSON.stringify([result]);
      
      console.log(resultText);
    } catch (err) {
      const resultText = "Test #" + (index + 1) + ": ✗ ERROR | " + err.message;
      console.log(resultText);
    }
  });
}

// Wait for the solution function to be defined
setTimeout(() => {
  if (typeof solution === 'function') {
    runFunctionTests(solution, functionCodingTests.publicTests);
  } else {
    console.log("✗ ERROR: You must define a function named 'solution'");
  }
}, 0);

// User code:
`

  // Get the file content
  let userCode = file.languageContent?.[selectedLanguage] || file.content || ""

  // Ensure it has a solution function template if not already present
  if (!userCode.includes("function solution")) {
    userCode += `
// Define your solution function here
function solution(array: any[]): any {
  // Your solution here
  // array is the entire input array
  return [];
}
`
  }

  // Create modified file with test harness
  const fullTestHarness = testHarnessCode + testCasesCode + testHarnessCode2
  const modifiedFile = {
    ...file,
    content: fullTestHarness + userCode,
    languageContent: {
      ...file.languageContent,
      [selectedLanguage]: fullTestHarness + userCode,
    },
  }

  // Create a file collection with the modified file
  const modifiedFiles = files.map((f) => (f.id === fileId ? modifiedFile : f))

  // Get the appropriate executor
  const executor = getExecutor(selectedLanguage)

  // Capture test output
  let testOutput: string[] = []

  // Create execution context
  const context: ExecutionContext = {
    files: modifiedFiles,
    selectedLanguage,
    addOutput: (output) => {
      if (Array.isArray(output)) {
        testOutput = [...testOutput, ...output]
      } else {
        testOutput = [...testOutput, output]
      }
      addOutput(output)
    },
    clearTerminal,
    setIsExecuting,
  }

  try {
    // Execute the code with the test harness
    await executor.execute(fileId, context)

    // Parse the output to extract test results
    const outputText = testOutput.join("\n")

    // Process each test case
    fileCases.forEach((testCase, index) => {
      // Look for test result in the output
      const testPattern = new RegExp(
        `Test #${index + 1}: (✓ PASS|✗ (FAIL|ERROR))([\\s\\S]*?)(?=Test #${index + 2}:|$)`,
        "g",
      )
      const match = testPattern.exec(outputText)

      if (match) {
        const passed = match[1].includes("PASS")
        const resultText = match[0]

        // Update test results
        setTestResults((prev) => {
          const fileResults = [...(prev[fileId] || [])]
          fileResults[index] = {
            passed,
            actual: resultText,
            expected: JSON.stringify(testCase.expectedReturn || []),
          }
          return {
            ...prev,
            [fileId]: fileResults,
          }
        })
      } else {
        // No match found, mark as failed
        setTestResults((prev) => {
          const fileResults = [...(prev[fileId] || [])]
          fileResults[index] = {
            passed: false,
            actual: "Test execution failed or output format invalid",
            expected: JSON.stringify(testCase.expectedReturn || []),
          }
          return {
            ...prev,
            [fileId]: fileResults,
          }
        })
      }
    })
  } catch (error) {
    console.error(`Function test execution error:`, error)
    const errorMessage = error instanceof Error ? error.message : String(error)
    addOutput(`Error: ${errorMessage}`)
  }
}
