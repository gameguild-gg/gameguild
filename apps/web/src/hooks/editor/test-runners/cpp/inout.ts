import type { TestCase, TestResult } from "../types"

/**
 * Run input/output tests for C++ code
 */
export async function runCppInoutTests(
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
    const testHarness = generateCppInoutTestHarness(code, tests)

    // Execute the test harness
    const output = await executeCpp(testHarness)

    // Parse the test results
    return parseCppTestResults(output)
  } catch (error) {
    console.error("Error running C++ inout tests:", error)
    return tests.map(() => ({
      passed: false,
      expected: "Test execution",
      actual: `Error: ${error}`,
    }))
  }
}

/**
 * Generate a C++ test harness for input/output tests
 */
function generateCppInoutTestHarness(code: string, tests: TestCase[]): string {
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
    const { args = [] } = test

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

    // Get expected return value
    const expectedReturn = test.expectedReturn
    let expectedReturnCode = ""

    if (Array.isArray(expectedReturn)) {
      // Convert array to vector
      const elements = expectedReturn
        .map((el) => {
          if (typeof el === "string") return `"${el}"`
          return el
        })
        .join(", ")
      expectedReturnCode = `std::vector<${typeof expectedReturn[0] === "string" ? "std::string" : "int"}>{${elements}}`
    } else if (typeof expectedReturn === "string") {
      expectedReturnCode = `"${expectedReturn}"`
    } else {
      expectedReturnCode = expectedReturn
    }

    // Add test case
    testHarness += `
        // Test ${index + 1}
        {
            try {
                auto result = solution(${argsCode});
                
                // Check result
                bool passed = false;
                std::string actual;
                std::string expected;
                
                // Convert result to string for display
                if constexpr (std::is_same_v<decltype(result), std::vector<int>> || 
                             std::is_same_v<decltype(result), std::vector<std::string>>) {
                    actual = vectorToString(result);
                    auto expectedVector = ${expectedReturnCode};
                    expected = vectorToString(expectedVector);
                    passed = vectorsEqual(result, expectedVector);
                } else {
                    actual = toString(result);
                    auto expectedValue = ${expectedReturnCode};
                    expected = toString(expectedValue);
                    passed = (result == expectedValue);
                }
                
                // Add result to testResults
                testResults.push_back({passed, expected, actual});
            } catch (const std::exception& e) {
                // Test threw an exception
                testResults.push_back({false, "No exception", std::string("Exception: ") + e.what()});
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
