import type { TestCase, TestResult } from "../types"

/**
 * Run predicate tests for C header files
 */
export async function runPredicateTests(
  code: string,
  tests: TestCase[],
  executeC: (code: string) => Promise<string>,
): Promise<TestResult[]> {
  // Extract the function name from the header file
  const functionNameMatch = code.match(/\b(\w+)\s*$$[^)]*$$\s*;/)
  const functionName = functionNameMatch ? functionNameMatch[1] : "solution"

  // Create a C file that includes the header and runs the tests
  const testHarness = generateCHeaderPredicateTestHarness(code, functionName, tests)

  try {
    // Execute the test harness
    const output = await executeC(testHarness)

    // Parse the test results from the output
    return parseTestResults(output, tests)
  } catch (error) {
    // Handle execution errors
    return tests.map((test) => ({
      passed: false,
      message: `Error: ${error instanceof Error ? error.message : String(error)}`,
      expected: test.predicate || "Unknown predicate",
      actual: "Error",
      testCase: test,
    }))
  }
}

/**
 * Generate a C test harness for header files with predicate tests
 */
function generateCHeaderPredicateTestHarness(headerCode: string, functionName: string, tests: TestCase[]): string {
  // Create a test harness that includes the header file
  return `
// Include standard libraries
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

// Define HEADER_IMPLEMENTATION to include the implementation
#define HEADER_IMPLEMENTATION

// Include the header code directly
${headerCode}

// Helper function to compare arrays
bool compare_arrays(const int* arr1, const int* arr2, int size) {
  for (int i = 0; i < size; i++) {
    if (arr1[i] != arr2[i]) return false;
  }
  return true;
}

// Helper function to print arrays
void print_array(const int* arr, int size) {
  printf("[");
  for (int i = 0; i < size; i++) {
    printf("%d", arr[i]);
    if (i < size - 1) printf(", ");
  }
  printf("]");
}

// Main function to run tests
int main() {
  printf("TEST_RESULTS_START\\n");
  
  int total_tests = ${tests.length};
  int passed_tests = 0;
  
  ${tests
    .map((test, index) => {
      // Convert test arguments to C
      const args = test.args
        .map((arg) => {
          if (Array.isArray(arg)) {
            return `(int[]){${arg.join(", ")}}`
          } else if (typeof arg === "string") {
            return `"${arg}"`
          } else {
            return String(arg)
          }
        })
        .join(", ")

      // Convert JavaScript predicate to C condition
      const predicate = test.predicate || ""
      let condition = "false"

      if (predicate.includes("=>")) {
        // Arrow function
        const body = predicate.split("=>")[1].trim()
        condition = convertJsConditionToC(body, "result")
      } else if (predicate.includes("return")) {
        // Regular function
        const returnStatement = predicate.match(/return\s+(.*?);/)
        if (returnStatement) {
          condition = convertJsConditionToC(returnStatement[1], "result")
        }
      }

      return `
    // Test ${index + 1}
    {
      int result = ${functionName}(${args});
      bool passed = ${condition};
      
      printf("Test %d: %s\\n", ${index + 1}, passed ? "PASSED" : "FAILED");
      if (!passed) {
        printf("  Predicate: ${predicate.replace(/"/g, '\\"')}\\n");
        printf("  Result: %d\\n", result);
      }
      
      passed_tests += passed ? 1 : 0;
    }`
    })
    .join("\n")}
  
  printf("\\nPassed %d out of %d tests\\n", passed_tests, total_tests);
  printf("TEST_RESULTS_END\\n");
  
  return 0;
}
`
}

/**
 * Convert JavaScript condition to C condition
 */
function convertJsConditionToC(jsCondition: string, resultVar: string): string {
  return jsCondition
    .replace(/===|==/g, "==")
    .replace(/!==|!=/g, "!=")
    .replace(/&&/g, "&&")
    .replace(/\|\|/g, "||")
    .replace(/!/g, "!")
    .replace(/\bx\b/g, resultVar)
    .replace(/\btrue\b/g, "true")
    .replace(/\bfalse\b/g, "false")
}

/**
 * Parse test results from the output
 */
function parseTestResults(output: string, tests: TestCase[]): TestResult[] {
  const results: TestResult[] = []

  // Extract the test results section
  const resultsMatch = output.match(/TEST_RESULTS_START\n([\s\S]*?)\nTEST_RESULTS_END/)
  if (!resultsMatch) {
    return tests.map((test) => ({
      passed: false,
      message: "Could not parse test results",
      expected: test.predicate || "Unknown predicate",
      actual: "Unknown",
      testCase: test,
    }))
  }

  const resultsText = resultsMatch[1]

  // Parse individual test results
  const testLines = resultsText.split("\n").filter((line) => line.match(/Test \d+: (PASSED|FAILED)/))

  testLines.forEach((line, index) => {
    if (index >= tests.length) return

    const passed = line.includes("PASSED")
    const test = tests[index]

    if (passed) {
      results.push({
        passed: true,
        message: "Test passed",
        expected: test.predicate || "Unknown predicate",
        actual: "Predicate satisfied",
        testCase: test,
      })
    } else {
      // Find the result line for this test
      const resultLineIndex = resultsText.indexOf(`Test ${index + 1}: FAILED`)
      const resultLine = resultsText
        .substring(resultLineIndex)
        .split("\n")
        .find((line) => line.includes("Result:"))
      const actual = resultLine ? resultLine.replace("Result:", "").trim() : "Unknown"

      results.push({
        passed: false,
        message: `Test failed: predicate ${test.predicate} not satisfied with result ${actual}`,
        expected: test.predicate || "Unknown predicate",
        actual,
        testCase: test,
      })
    }
  })

  // Fill in any missing results
  while (results.length < tests.length) {
    const test = tests[results.length]
    results.push({
      passed: false,
      message: "Test not executed",
      expected: test.predicate || "Unknown predicate",
      actual: "Unknown",
      testCase: test,
    })
  }

  return results
}
