import { getExecutor } from "../../executors/executor-factory"
import type { TestRunnerOptions } from "../types"

export async function runInOutTestsJavaScript(options: TestRunnerOptions) {
  const { fileId, file, fileCases, files, selectedLanguage, addOutput, clearTerminal, setIsExecuting, setTestResults } =
    options

  // Create a modified file with the test harness prepended
  const testHarnessCode = `
// System Test Code:

// Tests based on user-defined test cases
const functionCodingTests = {
  publicTests: ${JSON.stringify(
    fileCases.map((test) => ({
      args: test.args || [],
      expectedReturn: test.expectedReturn || [],
    })),
  )}
};

// Deep comparison function
function deepEqual(a, b) {
  return JSON.stringify(a) === JSON.stringify(b);
}

// Execute tests with multiple inputs and outputs
function runFunctionTests(solution, tests) {
  let allResults = [];
  
  tests.forEach((test, index) => {
    try {
      // Ensure args is always an array
      const args = Array.isArray(test.args) ? test.args : [];
      // Spread the args array to pass multiple arguments to the solution function
      const result = solution(...args);
      const passed = deepEqual(result, test.expectedReturn[0]);
      
      // Format each argument individually
      const argsDisplay = args.map((arg, argIndex) => 
        \`Arg\${argIndex + 1}: \${JSON.stringify(arg)}\`
      ).join(' | ');

      // More compact, English output format
      const resultText = 
        \`Test #\${index + 1}: \${passed ? "✓ PASS" : "✗ FAIL"}\\n\` +
        \`\${argsDisplay} | Expected: \${JSON.stringify(test.expectedReturn[0])} | Got: \${JSON.stringify(result)}\`;
      
      console.log(resultText);
      allResults.push({ index, passed, resultText });
    } catch (err) {
      const resultText = \`Test #\${index + 1}: ✗ ERROR | \${err.message}\`;
      console.log(resultText);
      allResults.push({ index, passed: false, resultText });
    }
  });
  
  return allResults;
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
function solution(a, b, c) {
  // Your solution here
  // You can use multiple parameters (a, b, c, etc.)
  // Modify the parameter list as needed for your function
  return a;
}
`
  }

  // Create modified file with test harness
  const modifiedFile = {
    ...file,
    content: testHarnessCode + userCode,
    languageContent: {
      ...file.languageContent,
      [selectedLanguage]: testHarnessCode + userCode,
    },
  }

  // Create a file collection with the modified file
  const modifiedFiles = files.map((f) => (f.id === fileId ? modifiedFile : f))

  // Get the appropriate executor
  const executor = getExecutor(selectedLanguage)

  // Capture test output
  let testOutput: string[] = []

  // Create execution context
  const context = {
    files: modifiedFiles,
    selectedLanguage,
    addOutput: (output: string | string[]) => {
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
