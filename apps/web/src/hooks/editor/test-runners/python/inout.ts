import type { TestRunnerOptions } from '../types';

interface LocalExecutionContext {
  files: Array<{
    id: string;
    name: string;
    content: string;
  }>;
  selectedLanguage: string;
  addOutput: (output: string | string[]) => void;
  clearTerminal: () => void;
  setIsExecuting: (isExecuting: boolean) => void;
}

/**
 * Run In/Out tests for Python code
 */
export async function runInOutTestsPython(options: TestRunnerOptions): Promise<void> {
  const { fileId, file, fileCases, files, addOutput, setIsExecuting, setTestResults } = options;

  try {
    addOutput(`Running In/Out tests for Python...`);

    // Create a test harness in Python that will run all the tests
    const testPythonScript = `
import json
import sys
import traceback

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

# Deep equality comparison
def deep_equals(a, b):
    if type(a) != type(b):
        return False
    if isinstance(a, list):
        if len(a) != len(b):
            return False
        return all(deep_equals(a[i], b[i]) for i in range(len(a)))
    if isinstance(a, dict):
        if set(a.keys()) != set(b.keys()):
            return False
        return all(deep_equals(a[k], b[k]) for k in a)
    return a == b

# Get the solution function
solution_func = None
for name, obj in list(globals().items()):
    if callable(obj) and name != "deep_equals" and name != "serialize":
        solution_func = obj
        break

if not solution_func:
    print("Error: No solution function found")
    sys.exit(1)

print(f"Testing function: {solution_func.__name__}")

# Run each test case
for i, test in enumerate(test_cases):
    if test["type"] != "inout":
        continue
        
    args = test.get("args", [])
    expected_returns = test.get("expectedReturn", [])
    expected = expected_returns[0] if expected_returns else None
    
    test_result = {
        "passed": False,
        "actual": "",
        "expected": serialize(expected)
    }
    
    print(f"Test #{i+1}: {solution_func.__name__}({serialize(args)[1:-1]}) => {serialize(expected)}")
    
    try:
        # Call the solution function with the arguments
        if len(args) == 1 and isinstance(args[0], list):
            # If there's only one argument and it's a list, we assume it's an array
            # that should be expanded as multiple arguments
            actual = solution_func(*args[0])
        else:
            actual = solution_func(*args)
        
        # Check if the result matches the expected value
        passed = deep_equals(actual, expected)
        
        test_result["passed"] = passed
        test_result["actual"] = serialize(actual)
        
        print(f"  {'✓' if passed else '✗'} {'PASSED' if passed else 'FAILED'}")
        if not passed:
            print(f"  Expected: {serialize(expected)}")
            print(f"  Actual:   {serialize(actual)}")
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
    const testResults: { passed: boolean; actual: string; expected: string }[] = fileCases.map(() => ({
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

    // Create an execution context
    const context: LocalExecutionContext = {
      files: [...files, testFile],
      selectedLanguage: 'python',
      addOutput,
      clearTerminal: () => {},
      setIsExecuting,
    };

    // Execute the test script
    await executor.execute('temp-test-file', context as ExecutionContext);

    setTestResults({ [fileId]: testResults });
  } catch (error) {
    addOutput(`Error running tests: ${error instanceof Error ? error.message : String(error)}`);
  } finally {
    setIsExecuting(false);
  }
}

// Helper function to get the right executor
function getExecutor(language: string) {
  // This is a simplified version - in a real implementation, you would import the actual executor
  return {
    execute: async (fileId: string, context: ExecutionContext) => {
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
