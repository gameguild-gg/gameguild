import type { TestRunnerOptions } from '../types';

/**
 * Run input/output tests for C code
 */
export async function runCInOutTests(options: TestRunnerOptions): Promise<void> {
  const { file, fileCases, addOutput, setTestResults, setIsExecuting } = options;

  try {
    // Extract the function name from the code
    const functionNameMatch = file.content.match(/\s*(\w+)\s+(\w+)\s*$$[^)]*$$\s*{/);
    const functionName = functionNameMatch ? functionNameMatch[2] : 'solution';

    // Skip main function if it exists
    const mainFunctionName = functionName === 'main' ? 'solution' : functionName;

    // Create a test harness for C
    const testHarness = `
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

// Original code
${file.content}

// Test utilities
void print_array(int* arr, int size) {
  printf("[");
  for (int i = 0; i < size; i++) {
    printf("%d", arr[i]);
    if (i < size - 1) printf(", ");
  }
  printf("]");
}

bool arrays_equal(int* arr1, int size1, int* arr2, int size2) {
  if (size1 != size2) return false;
  for (int i = 0; i < size1; i++) {
    if (arr1[i] != arr2[i]) return false;
  }
  return true;
}

// Test harness
int main() {
  printf("TEST_RESULTS_START\\n");
  
  ${fileCases
    .map((test, index) => {
      const testNumber = index + 1;
      const args = test.args || [];
      const expectedReturn = test.expectedReturn;

      // Generate argument declarations
      const argDeclarations = args
        .map((arg, i) => {
          if (typeof arg === 'number') {
            return `int arg${i} = ${arg};`;
          } else if (typeof arg === 'string') {
            return `char* arg${i} = "${arg}";`;
          } else if (Array.isArray(arg)) {
            return `int arg${i}[] = {${arg.join(', ')}};
          int arg${i}_size = ${arg.length};`;
          } else {
            return `// Unsupported argument type`;
          }
        })
        .join('\n    ');

      // Generate function call
      const functionCall = `${mainFunctionName}(${args
        .map((arg, i) => {
          if (Array.isArray(arg)) {
            return `arg${i}, arg${i}_size`;
          } else {
            return `arg${i}`;
          }
        })
        .join(', ')})`;

      // Generate result checking
      let resultCheck = '';
      if (typeof expectedReturn === 'number') {
        resultCheck = `
    int expected = ${expectedReturn};
    bool passed = (result == expected);
    printf("{\\"test\\": %d, \\"passed\\": %s, \\"expected\\": \\"%d\\", \\"actual\\": \\"%d\\"}\\n",
           ${testNumber}, passed ? "true" : "false", expected, result);`;
      } else if (typeof expectedReturn === 'string') {
        resultCheck = `
    char* expected = "${expectedReturn}";
    bool passed = (strcmp(result, expected) == 0);
    printf("{\\"test\\": %d, \\"passed\\": %s, \\"expected\\": \\"%s\\", \\"actual\\": \\"%s\\"}\\n",
           ${testNumber}, passed ? "true" : "false", expected, result);`;
      } else if (Array.isArray(expectedReturn)) {
        resultCheck = `
    int expected[] = {${expectedReturn.join(', ')}};
    int expected_size = ${expectedReturn.length};
    bool passed = arrays_equal(result, result_size, expected, expected_size);
    
    printf("{\\"test\\": %d, \\"passed\\": %s, \\"expected\\": \\"", ${testNumber}, passed ? "true" : "false");
    print_array(expected, expected_size);
    printf("\\", \\"actual\\": \\"");
    print_array(result, result_size);
    printf("\\"}\n");`;
      } else {
        resultCheck = `
    printf("{\\"test\\": %d, \\"passed\\": false, \\"expected\\": \\"unsupported\\", \\"actual\\": \\"unsupported\\"}\\n",
           ${testNumber});`;
      }

      return `
  // Test ${testNumber}
  {
    // Prepare arguments
    ${argDeclarations}
    
    // Call function
    int result = ${functionCall};
    
    // Check result
    ${resultCheck}
  }`;
    })
    .join('\n')}
  
  printf("TEST_RESULTS_END\\n");
  return 0;
}
`;

    // Execute the test harness
    addOutput('Compiling and running C tests...');

    // Get the C executor
    const executor = getExecutor();

    // Create execution context
    const context = {
      files: options.files,
      selectedLanguage: 'c',
      addOutput,
      clearTerminal: options.clearTerminal,
      setIsExecuting,
    };

    // Execute the test harness
    const output = await executor.executeCode(testHarness, context);

    // Parse the test results
    if (!output) {
      throw new Error('Failed to execute C code');
    }

    const results = parseTestResults(output);

    // Update the test results
    if (results.length > 0) {
      setTestResults({
        [file.id]: results,
      });
    }

    // Add the output to the terminal
    if (!output) {
      throw new Error('Failed to execute C code');
    }

    addOutput(output);
  } catch (error) {
    addOutput(`Error running C tests: ${error}`);
    setIsExecuting(false);
  }
}

/**
 * Parse test results from the output
 */
function parseTestResults(output: string): { passed: boolean; actual: string; expected: string }[] {
  const results: { passed: boolean; actual: string; expected: string }[] = [];

  // Extract the test results section
  const startMarker = 'TEST_RESULTS_START';
  const endMarker = 'TEST_RESULTS_END';

  const startIndex = output.indexOf(startMarker);
  const endIndex = output.indexOf(endMarker);

  if (startIndex === -1 || endIndex === -1) {
    return results;
  }

  const resultsSection = output.substring(startIndex + startMarker.length, endIndex).trim();
  const lines = resultsSection.split('\n');

  for (const line of lines) {
    try {
      const result = JSON.parse(line);
      results.push({
        passed: result.passed,
        actual: result.actual,
        expected: result.expected,
      });
    } catch {
      // Skip lines that aren't valid JSON
    }
  }

  return results;
}

interface Executor {
  executeCode: (code: string, context: unknown) => Promise<string | null>;
}

/**
 * Get the C executor
 */
function getExecutor(): Executor {
  // This is a placeholder - in a real implementation, this would use the actual executor factory
  return {
    executeCode: async () => {
      // Simulate C execution with sample output
      return `
Compiling C code...
Running tests...
TEST_RESULTS_START
{"test": 1, "passed": true, "expected": "42", "actual": "42"}
{"test": 2, "passed": false, "expected": "100", "actual": "99"}
TEST_RESULTS_END
Execution complete.
`;
    },
  };
}
