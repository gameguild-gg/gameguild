import type { TestCase, TestResult } from '../types';
import { formatTestOutput } from '../utils';

/**
 * Convert JavaScript predicate to C++ condition
 */
function convertPredicateToCpp(predicate: string): string {
  // Remove arrow function syntax and convert to C++ lambda
  const cppPredicate = predicate
    .replace(/$$([^)]*)$$\s*=>\s*/, '')
    .replace(/===|===/g, '==')
    .replace(/!==|!==/g, '!=')
    .replace(/&&/g, '&&')
    .replace(/\|\|/g, '||')
    .replace(/!/g, '!')
    .replace(/\btrue\b/g, 'true')
    .replace(/\bfalse\b/g, 'false')
    .replace(/\bfunction\s*$$([^)]*)$$\s*{([\s\S]*?)return\s+(.*?);?\s*}/g, '$3');

  return cppPredicate;
}

/**
 * Run predicate tests for C++ header files
 */
export async function runHppPredicateTests(code: string, tests: TestCase[], executeCpp: (code: string) => Promise<string>): Promise<TestResult[]> {
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
        const { args, predicate } = test;

        // Handle different argument types
        const argsStr = args
          .map((arg) => {
            if (Array.isArray(arg)) {
              return `std::vector<int>{${arg.join(', ')}}`;
            } else if (typeof arg === 'string') {
              return `"${arg}"`;
            } else {
              return arg;
            }
          })
          .join(', ');

        // Convert JavaScript predicate to C++ condition
        const cppPredicate = convertPredicateToCpp(predicate || '');

        return `
    try {
        // Test ${index + 1}
        auto result = ${functionName}(${argsStr});
        bool passed = (${cppPredicate});
        
        json testResult;
        testResult["passed"] = passed;
        testResult["expected"] = "${predicate}";
        testResult["actual"] = passed ? "Predicate satisfied" : "Predicate not satisfied";
        testResult["error"] = "";
        results.push_back(testResult);
    } catch (const std::exception& e) {
        json testResult;
        testResult["passed"] = false;
        testResult["expected"] = "${predicate}";
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

  try {
    // Execute the test harness
    const output = await executeCpp(testHarness);

    // Parse the test results
    const resultsMatch = output.match(/TEST_RESULTS_START\s*([\s\S]*?)\s*TEST_RESULTS_END/);
    if (!resultsMatch) {
      return tests.map(() => ({
        passed: false,
        error: 'Failed to parse test results',
      }));
    }

    const resultsJson = resultsMatch[1].trim();
    const results = JSON.parse(resultsJson);

    return results.map((result: any, index: number) => ({
      passed: result.passed,
      expected: result.expected,
      actual: result.actual,
      error: result.error,
      output: formatTestOutput(tests[index], result),
    }));
  } catch (error) {
    return tests.map(() => ({
      passed: false,
      error: error instanceof Error ? error.message : String(error),
    }));
  }
}
