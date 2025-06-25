import { getExecutor } from "../../executors/executor-factory"
import type { TestRunnerOptions } from "../types"

export async function runPredicateTestsJavaScript(options: TestRunnerOptions) {
  const { fileId, file, fileCases, files, selectedLanguage, addOutput, clearTerminal, setIsExecuting, setTestResults } =
    options

  // Create a modified file with the test harness prepended
  const testHarnessCode = `
// System Test Code:

// Tests based on user-defined predicate tests
const predicateTests = ${JSON.stringify(
    fileCases.map((test) => ({
      input: test.args || [],
      rule: test.predicate || "result => false",
    })),
    (key, value) => {
      if (key === "rule" && typeof value === "string") {
        // Return the string as is, without quotes
        return value
      }
      return value
    },
  )};

// Execute tests with predicate validation
function runPredicateTests(solution, tests) {
  let allResults = [];
  
  tests.forEach((test, index) => {
    try {
      // Ensure input is always an array
      const input = Array.isArray(test.input) ? test.input : [];
      // Pass the entire array as a single parameter
      const output = solution(input);
      
      // Create a function from the predicate string
      let ruleFn;
      try {
        ruleFn = new Function('result', 'return (' + test.rule + ')(result)');
      } catch (err) {
        console.log(\`Test #\${index + 1}: ✗ FAIL | Invalid predicate function: \${err.message}\`);
        allResults.push({ index, passed: false, resultText: \`Invalid predicate function: \${err.message}\` });
        return;
      }
      
      // Execute the predicate function
      const passed = ruleFn(output);
      
      // More compact, English output format
      const resultText = 
        \`Test #\${index + 1}: \${passed ? "✓ PASS" : "✗ FAIL"} | \` +
        \`Input: \${JSON.stringify(input)} | \` +
        \`Result: \${JSON.stringify(output)} | \` +
        \`Predicate: \${test.rule}\`;
      
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
function solution(array) {
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
