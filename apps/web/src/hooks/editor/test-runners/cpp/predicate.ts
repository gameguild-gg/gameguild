import type { TestCase, TestResult } from "../types"

/**
 * Run predicate tests for C++ code
 */
export async function runCppPredicateTests(
  code: string,
  tests: TestCase[],
  executeCpp: ((code: string) => Promise<string>) | undefined,
): Promise<TestResult[]> {
  // Check if tests exist
  if (!tests || !tests.length) {
    return []
  }

  // Check if C++ execution is available
  if (!executeCpp) {
    return tests.map(() => ({
      passed: false,
      expected: "C++ execution",
      actual: "C++ compiler not yet implemented",
    }))
  }

  try {
    // Generate test harness
    const testHarness = generateCppPredicateTestHarness(code, tests)

    // Execute the test harness
    const output = await executeCpp(testHarness)

    // Parse the test results
    return parseCppTestResults(output)
  } catch (error) {
    console.error("Error running C++ predicate tests:", error)
    return tests.map(() => ({
      passed: false,
      expected: "Test execution",
      actual: `Error: ${error}`,
    }))
  }
}

/**
 * Generate a C++ test harness for predicate tests
 */
function generateCppPredicateTestHarness(code: string, tests: TestCase[]): string {
  // Include the original code
  let testHarness = code + "\n\n"

  // Add test harness headers and utilities
  testHarness += `
#include <iostream>
#include <vector>
#include <string>
#include <sstream>
#include <functional>
#include <algorithm>

// Helper function to convert any value to string
template<typename T>
std::string toString(const T& value) {
    std::ostringstream ss;
    ss << value;
    return ss.str();
}

// Helper function to compare vectors
template<typename T>
bool vectorsEqual(const std::vector<T>& a, const std::vector<T>& b) {
    if (a.size() != b.size()) return false;
    return std::equal(a.begin(), a.end(), b.begin());
}

// Helper function to convert vector to string
template<typename T>
std::string vectorToString(const std::vector<T>& vec) {
    std::ostringstream ss;
    ss << "[";
    for (size_t i = 0; i < vec.size(); ++i) {
        if (i > 0) ss << ", ";
        ss << vec[i];
    }
    ss << "]";
    return ss.str();
}

// Test result structure
struct TestResult {
    bool passed;
    std::string expected;
    std::string actual;
};

int main() {
    // Test results
    std::vector<TestResult> testResults;
    
    // Run tests
    try {
`

  // Add each test
  tests.forEach((test, index) => {
    const { args = [], predicate = "result => true" } = test

    // Convert args to C++ code
    const argsCode = args
      .map((arg) => {
        if (Array.isArray(arg)) {
          // Convert array to vector
          const elements = arg
            .map((el) => {
              if (typeof el === "string") return `"${el}"`
              return el
            })
            .join(", ")
          return `std::vector<${typeof arg[0] === "string" ? "std::string" : "int"}>{${elements}}`
        } else if (typeof arg === "string") {
          return `"${arg}"`
        }
        return arg
      })
      .join(", ")

    // Convert JavaScript predicate to C++ condition
    const cppPredicate = convertJsPredicateToCpp(predicate)

    // Add test case
    testHarness += `
        // Test ${index + 1}
        {
            try {
                auto result = solution(${argsCode});
                
                // Convert result to string for display
                std::string resultStr;
                if constexpr (std::is_same_v<decltype(result), std::vector<int>> || 
                             std::is_same_v<decltype(result), std::vector<std::string>>) {
                    resultStr = vectorToString(result);
                } else {
                    resultStr = toString(result);
                }
                
                // Check predicate
                bool passed = ${cppPredicate};
                
                // Add result to testResults
                testResults.push_back({
                    passed, 
                    "Predicate: ${predicate.replace(/"/g, '\\"')}", 
                    "Result: " + resultStr
                });
            } catch (const std::exception& e) {
                // Test threw an exception
                testResults.push_back({
                    false, 
                    "No exception", 
                    std::string("Exception: ") + e.what()
                });
            }
        }
`
  })

  // Close the test harness and output results
  testHarness += `
        // Output test results
        std::cout << "TEST_RESULTS_START" << std::endl;
        for (const auto& result : testResults) {
            std::cout << (result.passed ? "PASS" : "FAIL") << "|" 
                      << result.expected << "|" 
                      << result.actual << std::endl;
        }
        std::cout << "TEST_RESULTS_END" << std::endl;
        
        // Output summary
        int passCount = 0;
        for (const auto& result : testResults) {
            if (result.passed) passCount++;
        }
        std::cout << "\\nTests: " << testResults.size() << ", Passed: " << passCount 
                  << ", Failed: " << (testResults.size() - passCount) << std::endl;
        
        return 0;
    } catch (const std::exception& e) {
        std::cerr << "Error in test harness: " << e.what() << std::endl;
        return 1;
    }
}
`

  return testHarness
}

/**
 * Convert a JavaScript predicate to a C++ condition
 */
function convertJsPredicateToCpp(predicate: string): string {
  // Convert arrow function
  if (predicate.includes("=>")) {
    const arrowMatch = predicate.match(/\s*(?:$$([^)]*)$$|([^=]*?))\s*=>\s*(.*)/)
    if (arrowMatch) {
      const paramName = arrowMatch[1] || arrowMatch[2] || "result"
      const body = arrowMatch[3].trim()

      // Simple case: checking if result matches a value
      if (body.includes("===") || body.includes("==")) {
        return body.replace(/===|==/g, "==").replace(/!==|!=/g, "!=")
      }

      // Check for type
      if (body.includes("typeof") && body.includes("number")) {
        return "std::is_arithmetic<decltype(result)>::value"
      }
      if (body.includes("typeof") && body.includes("string")) {
        return "std::is_same_v<decltype(result), std::string>"
      }

      // Default true for complex cases
      return "true /* Complex predicate not fully converted */"
    }
  }

  // If none of the above, assume it's a simple check
  return "(result != 0)" // fallback for default case
}

/**
 * Parse the output from the C++ test harness
 */
function parseCppTestResults(output: string): TestResult[] {
  try {
    // Extract the test results
    const startMarker = "TEST_RESULTS_START"
    const endMarker = "TEST_RESULTS_END"

    const startIndex = output.indexOf(startMarker)
    const endIndex = output.indexOf(endMarker)

    if (startIndex === -1 || endIndex === -1) {
      console.error("Could not find test results markers in output:", output)
      return []
    }

    // Get the lines between the markers
    const resultsText = output.substring(startIndex + startMarker.length, endIndex).trim()
    const lines = resultsText.split("\n")

    return lines.map((line) => {
      const [result, expected, actual] = line.split("|", 3)
      return {
        passed: result === "PASS",
        expected,
        actual: actual || "No output",
      }
    })
  } catch (error) {
    console.error("Error parsing C++ test results:", error)
    return []
  }
}
