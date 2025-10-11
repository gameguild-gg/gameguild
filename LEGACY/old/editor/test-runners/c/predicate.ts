import type { TestRunnerOptions } from '../types';

/**
 * Run predicate tests for C code
 */
export async function runCPredicateTests(options: TestRunnerOptions): Promise<void> {
  const { file, fileCases, addOutput, setTestResults, setIsExecuting } = options;

  try {
    // Extract the function name from the code
    const functionNameMatch = file.content.match(/\s*(\w+)\s+(\w+)\s*$$[^)]*$$\s*{/);
    const functionName = functionNameMatch ? functionNameMatch[2] : 'solution';

    // Skip main function if it exists
    const mainFunctionName = functionName === 'main' ? 'solution' : functionName;

    // Create a test harness for C with predicates
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

// Test harness
int main() {
  printf("TEST_RESULTS_START\\n");
  
  ${fileCases
    .map((test, index) => {
      const testNumber = index + 1;
      const args = test.args || [];
      const predicate = convertPredicateToC(test.predicate || '');

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

      return `
  // Test ${testNumber}
  {
    // Prepare arguments
    ${argDeclarations}
    
    // Call function
    int result = ${functionCall};
    
    // Check predicate
    bool passed = (${predicate});
    
    printf("{\\"test\\": %d, \\"passed\\": %s, \\"expected\\": \\"Predicate should be satisfied\\", \\"actual\\": \\"%s\\"}\\n",
           ${testNumber}, passed ? "true" : "false", passed ? "Predicate satisfied" : "Predicate not satisfied");
  }`;
    })
    .join('\n')}
  
  printf("TEST_RESULTS_END\\n");
  return 0;
}
`;

    // Execute the test harness
    addOutput('Compiling and running C predicate tests...');

    // Get the C executor
    const executor = getExecutor('c');

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
    const results = parseTestResults(output);

    // Update the test results
    if (results.length > 0) {
      setTestResults({
        [file.id]: results,
      });
    }

    // Add the output to the terminal
    addOutput(output);
  } catch (error) {
    addOutput(`Error running C predicate tests: ${error}`);
    setIsExecuting(false);
  }
}

/**
 * Convert a JavaScript predicate to C syntax
 */
function convertPredicateToC(predicate: string): string {
  // Replace JavaScript syntax with C syntax
  return predicate
    .replace(/===|==/g, '==')
    .replace(/!==|!=/g, '!=')
    .replace(/&&/g, '&&')
    .replace(/\|\|/g, '||')
    .replace(/!/g, '!')
    .replace(/\./g, '->') // For struct access
    .replace(/result/g, 'result'); // Keep result as is
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
    } catch (e) {
      // Skip lines that aren't valid JSON
    }
  }

  return results;
}

/**
 * Get the C executor
 */
function getExecutor(language: string) {
  // This is a placeholder - in a real implementation, this would use the actual executor factory
  return {
    executeCode: async (code: string, context: any) => {
      // Simulate C execution with sample output
      return `
Compiling C code...
Running predicate tests...
TEST_RESULTS_START
{"test": 1, "passed": true, "expected": "Predicate should be satisfied", "actual": "Predicate satisfied"}
{"test": 2, "passed": false, "expected": "Predicate should be satisfied", "actual": "Predicate not satisfied"}
TEST_RESULTS_END
Execution complete.
`;
    },
  };
}
