import type { TestCase, TestResult } from "../types"

/**
 * Run input/output tests for C header files
 */
export async function runInOutTests(
  code: string,
  tests: TestCase[],
  executeC: (code: string) => Promise<string>,
): Promise<TestResult[]> {
  // Extract the function name from the header file
  const functionNameMatch = code.match(/\b(\w+)\s*$$[^)]*$$\s*;/)
  const functionName = functionNameMatch ? functionNameMatch[1] : "solution"

  // Create a C file that includes the header and runs the tests
  const testHarness = generateCHeaderTestHarness(code, functionName, tests)

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
      expected: String(test.expected),
      actual: "Error",
      testCase: test,
    }))
  }
}

/**
 * Generate a C test harness for header files
 */
function generateCHeaderTestHarness(headerCode: string, functionName: string, tests: TestCase[]): string {
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

      // Handle different expected result types
      let expectedDeclaration = ""
      let resultComparison = ""
      let resultOutput = ""

      if (Array.isArray(test.expected)) {
        const expectedSize = test.expected.length
        expectedDeclaration = `int expected${index}[] = {${test.expected.join(", ")}};`
        resultComparison = `compare_arrays(result, expected${index}, ${expectedSize})`
        resultOutput = `print_array(result, ${expectedSize}); printf(" vs "); print_array(expected${index}, ${expectedSize});`
      } else if (typeof test.expected === "string") {
        expectedDeclaration = `const char* expected${index} = "${test.expected}";`
        resultComparison = `strcmp(result, expected${index}) == 0`
        resultOutput = `printf("\\"%s\\" vs \\"%s\\"", result, expected${index});`
      } else {
        expectedDeclaration = `int expected${index} = ${test.expected};`
        resultComparison = `result == expected${index}`
        resultOutput = `printf("%d vs %d", result, expected${index});`
      }

      return `
    // Test ${index + 1}
    {
      ${expectedDeclaration}
      int result = ${functionName}(${args});
      bool passed = ${resultComparison};
      
      printf("Test %d: %s\\n", ${index + 1}, passed ? "PASSED" : "FAILED");
      if (!passed) {
        printf("  Expected: ");
        ${resultOutput}
        printf("\\n");
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
      expected: String(test.expected),
      actual: "Unknown",
      testCase: test,
    }))
  }

  const resultsText = resultsMatch[1]

  // Parse individual test results
  const testLines = resultsText.match(/Test \d+: (PASSED|FAILED)(\n {2}Expected: .*)?/g) || []

  testLines.forEach((line, index) => {
    if (index >= tests.length) return

    const passed = line.includes("PASSED")
    const test = tests[index]

    if (passed) {
      results.push({
        passed: true,
        message: "Test passed",
        expected: String(test.expected),
        actual: String(test.expected),
        testCase: test,
      })
    } else {
      const expectedActualMatch = line.match(/Expected: (.*)/)
      const actual = expectedActualMatch ? expectedActualMatch[1].trim() : "Unknown"

      results.push({
        passed: false,
        message: `Test failed: expected ${test.expected}, got ${actual}`,
        expected: String(test.expected),
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
      expected: String(test.expected),
      actual: "Unknown",
      testCase: test,
    })
  }

  return results
}
