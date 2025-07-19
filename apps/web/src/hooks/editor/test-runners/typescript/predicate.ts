import type { TestRunner, TestRunnerOptions } from "../types"
import { getExecutor } from "../../executors/executor-factory"
import type { ExecutionContext } from "../../executors/types"

/**
 * Run Predicate tests for TypeScript
 */
export const runPredicateTestsTypeScript: TestRunner = async (options: TestRunnerOptions) => {
  const { fileId, file, fileCases, files, selectedLanguage, addOutput, clearTerminal, setIsExecuting, setTestResults } =
    options

  // Sanitize predicate strings to ensure they're valid function expressions
  const sanitizedCases = fileCases.map((testCase) => {
    let predicate = testCase.predicate || "result => false"

    // Basic sanitization to avoid syntax errors
    predicate = predicate.replace(/[\r\n]/g, " ") // Remove line breaks

    return {
      ...testCase,
      predicate,
    }
  })

  // Start of test harness code
  let testHarnessCode = "// System Test Code:\n\n"

  // Add test cases array
  testHarnessCode += "// Test cases\n"
  testHarnessCode += "const predicateTests = [\n"

  // Add each test case
  sanitizedCases.forEach((test, index) => {
    const isLast = index === sanitizedCases.length - 1
    testHarnessCode += "  {\n"
    testHarnessCode += "    input: " + JSON.stringify(test.args || []) + ",\n"
    testHarnessCode += "    rule: " + JSON.stringify(test.predicate) + "\n"
    testHarnessCode += "  }" + (isLast ? "" : ",") + "\n"
  })

  testHarnessCode += "];\n\n"

  // Add test runner function
  testHarnessCode += `
// Execute tests with predicate validation
function runPredicateTests(solution, tests) {
  tests.forEach((test, index) => {
    try {
      // Ensure input is always an array
      const input = Array.isArray(test.input) ? test.input : [];
      
      // Pass the entire array as a single parameter
      const output = solution(input);
      
      // Safely evaluate the predicate
      let passed = false;
      try {
        // Properly evaluate different predicate formats
        if (test.rule.includes("=>")) {
          // Arrow function
          const arrowFunc = eval("(" + test.rule + ")");
          passed = arrowFunc(output);
        } else if (test.rule.startsWith("function")) {
          // Function expression
          const funcExpr = eval("(" + test.rule + ")");
          passed = funcExpr(output);
        } else {
          // Simple expression - wrap in a function
          const simpleFunc = new Function("result", "return " + test.rule);
          passed = simpleFunc(output);
        }
      } catch (err) {
        console.log("Test #" + (index + 1) + ": ✗ FAIL | Invalid predicate: " + err.message);
        return;
      }
      
      // Output the result
      const resultText = "Test #" + (index + 1) + ": " + (passed ? "✓ PASS" : "✗ FAIL") + 
                         " | Input: " + JSON.stringify(input) + 
                         " | Result: " + JSON.stringify(output) + 
                         " | Predicate: " + test.rule;
      
      console.log(resultText);
    } catch (err) {
      console.log("Test #" + (index + 1) + ": ✗ ERROR | " + err.message);
    }
  });
}

// Wait for the solution function to be defined
setTimeout(function() {
  if (typeof solution === 'function') {
    runPredicateTests(solution, predicateTests);
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
    sanitizedCases.forEach((testCase, index) => {
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
            expected: testCase.predicate || "",
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
            expected: testCase.predicate || "",
          }
          return {
            ...prev,
            [fileId]: fileResults,
          }
        })
      }
    })
  } catch (error) {
    console.error(`Predicate test execution error:`, error)
    const errorMessage = error instanceof Error ? error.message : String(error)
    addOutput(`Error: ${errorMessage}`)
  }
}
