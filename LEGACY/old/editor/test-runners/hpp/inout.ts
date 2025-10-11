import type { TestCase, TestResult } from '../types';
import { formatTestOutput } from '../utils';

/**
 * Run input/output tests for C++ header files
 */
export async function runHppInoutTests(code: string, tests: TestCase[], executeCpp: (code: string) => Promise<string>): Promise<TestResult[]> {
  // Extract function name from the header
  const functionMatch = code.match(/\b(\w+)\s*$$[^)]*$$\s*;/);
  const functionName = functionMatch ? functionMatch[1] : 'solution';

  // Create a test harness that includes the header and tests the function
  const testHarness = `
#define HEADER_IMPLEMENTATION
${code}

#include <iostream>
#include <vector>
#include <string>
#include <sstream>
#include <functional>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

// Helper function to compare vectors
template<typename T>
bool compareVectors(const std::vector<T>& a, const std::vector<T>& b) {
    if (a.size() != b.size()) return false;
    for (size_t i = 0; i < a.size(); i++) {
        if (a[i] != b[i]) return false;
    }
    return true;
}

// Helper function to convert vector to string
template<typename T>
std::string vectorToString(const std::vector<T>& vec) {
    std::ostringstream oss;
    oss << "[";
    for (size_t i = 0; i < vec.size(); i++) {
        if (i > 0) oss << ", ";
        oss << vec[i];
    }
    oss << "]";
    return oss.str();
}

int main() {
    std::cout << "TEST_RESULTS_START" << std::endl;
    json results = json::array();
    
    ${tests
      .map((test, index) => {
        const { args, expectedReturn } = test;

        // Handle different argument types
        const argsStr = args
          ? args
              .map((arg) => {
                if (Array.isArray(arg)) {
                  return `std::vector<int>{${arg.join(', ')}}`;
                } else if (typeof arg === 'string') {
                  return `"${arg}"`;
                } else {
                  return arg;
                }
              })
              .join(', ')
          : '';

        // Handle different expected return types
        let expectedStr;
        let comparisonCode;

        if (Array.isArray(expectedReturn)) {
          expectedStr = `std::vector<int>{${expectedReturn.join(', ')}}`;
          comparisonCode = `
        bool passed = compareVectors(result, ${expectedStr});
        std::string resultStr = vectorToString(result);
        std::string expectedStr = vectorToString(${expectedStr});`;
        } else if (typeof expectedReturn === 'string') {
          expectedStr = `"${expectedReturn}"`;
          comparisonCode = `
        bool passed = (result == ${expectedStr});
        std::string resultStr = result;
        std::string expectedStr = ${expectedStr};`;
        } else if (expectedReturn !== undefined) {
          expectedStr = String(expectedReturn);
          comparisonCode = `
        bool passed = (result == ${expectedStr});
        std::string resultStr = std::to_string(result);
        std::string expectedStr = std::to_string(${expectedStr});`;
        } else {
          expectedStr = 'undefined';
          comparisonCode = `
        bool passed = false;
        std::string resultStr = "undefined";
        std::string expectedStr = "undefined";`;
        }

        return `
    try {
        // Test ${index + 1}
        auto result = ${functionName}(${argsStr});
        ${comparisonCode}
        
        json testResult;
        testResult["passed"] = passed;
        testResult["expected"] = ${expectedStr};
        testResult["actual"] = result;
        testResult["error"] = "";
        results.push_back(testResult);
    } catch (const std::exception& e) {
        json testResult;
        testResult["passed"] = false;
        testResult["expected"] = ${expectedStr};
        testResult["actual"] = "Error";
        testResult["error"] = e.what();
        results.push_back(testResult);
    }`;
      })
      .join('\n')}
    
    std::cout << results.dump(2) << std::endl;
    std::cout << "TEST_RESULTS_END" << std::endl;
    return 0;
}
`;

  interface TestResultData {
    passed: boolean;
    expected: string;
    actual: string;
    error?: string;
  }

  try {
    // Execute the test harness
    const output = await executeCpp(testHarness);

    // Parse the test results
    const resultsMatch = output.match(/TEST_RESULTS_START\s*([\s\S]*?)\s*TEST_RESULTS_END/);
    if (!resultsMatch) {
      return tests.map(() => ({
        passed: false,
        expected: 'Test execution',
        actual: 'Failed to parse test results',
        error: 'Failed to parse test results',
      }));
    }

    const resultsJson = resultsMatch[1].trim();
    const results = JSON.parse(resultsJson) as TestResultData[];

    return results.map((result, index) => ({
      passed: result.passed,
      expected: result.expected,
      actual: result.actual,
      error: result.error,
      output: formatTestOutput(tests[index], result),
    }));
  } catch (error) {
    return tests.map(() => ({
      passed: false,
      expected: 'Test execution',
      actual: 'Error occurred',
      error: error instanceof Error ? error.message : String(error),
    }));
  }
}
