import type { TestRunnerOptions } from '../types';

/**
 * Run Predicate tests for Python code
 */
export async function runPredicateTestsPython(options: TestRunnerOptions): Promise<void> {
  const { fileId, file, fileCases, files, selectedLanguage, addOutput, setIsExecuting, setTestResults, normalizeOutput } = options;

  try {
    addOutput(`Running Predicate tests for Python...`);

    // Create a test harness in Python that will run all the tests
    const testPythonScript = `
import json
import sys
import traceback
import re

# Original code
${file.content}

# Test cases
test_cases = ${JSON.stringify(fileCases)}

# Results array
results = []

# Helper function to serialize objects for display
def serialize(obj):
    try:
        return json.dumps(obj)
    except:
        return str(obj)

# Convert JavaScript predicate to Python
def convert_js_predicate(predicate):
    # Handle arrow functions: result => result > 0
    arrow_match = re.match(r'^\s*(\w+)\s*=>\s*(.+)$', predicate)
    if arrow_match:
        param, body = arrow_match.groups()
        # Convert JavaScript operators to Python syntax
        body = (
            body.replace('===', '==')
            .replace('!==', '!=')
            .replace('&&', ' and ')
            .replace('||', ' or ')
            .replace('!', ' not ')
            .replace('true', 'True')
            .replace('false', 'False')
            .replace('null', 'None')
            .replace('undefined', 'None')
        )
        return f"lambda {param}: {body}"
    
    # Handle function expressions: function(result) { return result > 0; }
    func_match = re.match(r'function\s*$$([^)]*)$$\s*\{([\s\S]*)\}', predicate)
    if func_match:
        param, body = func_match.groups()
        # Extract return statement
        return_match = re.search(r'return\s+([^;]+)', body)
        if return_match:
            return_expr = return_match.group(1)
            # Convert JavaScript operators to Python syntax
            return_expr = (
                return_expr.replace('===', '==')
                .replace('!==', '!=')
                .replace('&&', ' and ')
                .replace('||', ' or ')
                .replace('!', ' not ')
                .replace('true', 'True')
                .replace('false', 'False')
                .replace('null', 'None')
                .replace('undefined', 'None')
            )
            return f"lambda {param}: {return_expr}"
    
    # Default simple predicate
    return "lambda result: True"

# Get the solution function
solution_func = None
for name, obj in list(globals().items()):
    if callable(obj) and name != "serialize" and name != "convert_js_predicate":
        solution_func = obj
        break

if not solution_func:
    print("Error: No solution function found")
    sys.exit(1)

print(f"Testing function: {solution_func.__name__}")

# Run each test case
for i, test in enumerate(test_cases):
    if test["type"] != "predicate":
        continue
        
    args = test.get("args", [])
    predicate_str = test.get("predicate", "result => true")
    
    test_result = {
        "passed": False,
        "actual": "",
        "expected": predicate_str
    }
    
    print(f"Test #{i+1}: {solution_func.__name__}({serialize(args)[1:-1]}) => {predicate_str}")
    
    try:
        # Convert JavaScript predicate to Python
        python_predicate = convert_js_predicate(predicate_str)
        predicate_func = eval(python_predicate)
        
        # Call the solution function with the arguments
        if len(args) == 1 and isinstance(args[0], list):
            # If there's only one argument and it's a list, we assume it's an array
            # that should be expanded as multiple arguments
            actual = solution_func(*args[0])
        else:
            actual = solution_func(*args)
        
        # Check if the result satisfies the predicate
        passed = False
        try:
            passed = predicate_func(actual)
        except Exception as e:
            print(f"  ✗ ERROR in predicate: {str(e)}")
            test_result["actual"] = f"Error in predicate: {str(e)}"
        
        test_result["passed"] = passed
        test_result["actual"] = serialize(actual)
        
        print(f"  {'✓' if passed else '✗'} {'PASSED' if passed else 'FAILED'}")
        if not passed:
            print(f"  Predicate: {predicate_str}")
            print(f"  Result:    {serialize(actual)}")
    except Exception as e:
        print(f"  ✗ ERROR: {str(e)}")
        traceback.print_exc()
        test_result["actual"] = f"Error: {str(e)}"
    
    results.append(test_result)

# Print the results in a format that can be easily parsed
print("TEST_RESULTS_START")
print(json.dumps(results))
print("TEST_RESULTS_END")
`;

    // Execute the Python script
    const executor = getExecutor('python');
    let testResults: { passed: boolean; actual: string; expected: string }[] = fileCases.map(() => ({
      passed: false,
      actual: '',
      expected: '',
    }));

    // Create a temporary file to hold the test script
    const testFile: CodeFile = {
      id: 'temp-test-file',
      name: 'test.py',
      content: testPythonScript,
      isSelected: false,
      isEdited: false,
    };

    // Function to process output
    const processOutput = (output: string) => {
      // Extract the JSON results
      const resultsMatch = output.match(/TEST_RESULTS_START\n([\s\S]*?)\nTEST_RESULTS_END/);
      if (resultsMatch) {
        try {
          const parsedResults = JSON.parse(resultsMatch[1]);
          testResults = parsedResults;
          setTestResults({ [fileId]: testResults });
        } catch (error) {
          addOutput(`Error parsing test results: ${error}`);
        }
      } else {
        addOutput(`Error: Could not find test results in output`);
      }
    };

    // Create an execution context
    const context: ExecutionContext = {
      files: [...files, testFile],
      selectedLanguage: 'python',
      addOutput,
      clearTerminal: () => {},
      setIsExecuting,
    };

    // Execute the test script
    await executor.execute('temp-test-file', context, { processOutput });

    setTestResults({ [fileId]: testResults });
  } catch (error) {
    addOutput(`Error running tests: ${error.message}`);
  } finally {
    setIsExecuting(false);
  }
}

// Helper function to get the right executor
function getExecutor(language: string) {
  // This is a simplified version - in a real implementation, you would import the actual executor
  return {
    execute: async (fileId: string, context: ExecutionContext, options?: { processOutput: (output: string) => void }) => {
      // Mock implementation
      context.addOutput(`Executing ${language} code...`);
      // In a real implementation, this would execute the code
      return '';
    },
    isCompiled: false,
  };
}

// Simplified ExecutionContext type for this example
interface ExecutionContext {
  files: CodeFile[];
  selectedLanguage: string;
  addOutput: (output: string | string[]) => void;
  clearTerminal: () => void;
  setIsExecuting: (isExecuting: boolean) => void;
}

// Simplified CodeFile type for this example
interface CodeFile {
  id: string;
  name: string;
  content: string;
  isSelected: boolean;
  isEdited: boolean;
}
