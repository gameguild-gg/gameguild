import type { ProgrammingLanguage } from "../types"

// Export all template constants
export const DEFAULT_FIRST_CODE_TEMPLATES: Record<ProgrammingLanguage, string> = {
  javascript: `// Function test setup code
// Define test helper functions and setup here
function assertEquals(actual, expected, message = '') {
  if (actual !== expected) {
    throw new Error(\`Assertion failed \${message ? ': ' + message : ''}: expected \${expected}, got \${actual}\`);
  }
}

function assertArrayEquals(actual, expected, message = '') {
  if (!Array.isArray(actual) || !Array.isArray(expected) || actual.length !== expected.length) {
    throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
  }
  for (let i = 0; i < actual.length; i++) {
    if (actual[i] !== expected[i]) {
      throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
    }
  }
}
`,
  typescript: `// Function test setup code
// Define test helper functions and setup here
function assertEquals(actual: any, expected: any, message: string = ''): void {
  if (actual !== expected) {
    throw new Error(\`Assertion failed \${message ? ': ' + message : ''}: expected \${expected}, got \${actual}\`);
  }
}

function assertArrayEquals(actual: any[], expected: any[], message: string = ''): void {
  if (!Array.isArray(actual) || !Array.isArray(expected) || actual.length !== expected.length) {
    throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
  }
  for (let i = 0; i < actual.length; i++) {
    if (actual[i] !== expected[i]) {
      throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
    }
  }
}
`,
  python: `# Function test setup code
# Define test helper functions and setup here
def assert_equals(actual, expected, message=''):
    if actual != expected:
        raise AssertionError(f"Assertion failed{': ' + message if message else ''}: expected {expected}, got {actual}")

def assert_array_equals(actual, expected, message=''):
    if not isinstance(actual, list) or not isinstance(expected, list) or len(actual) != len(expected):
        raise AssertionError(f"Array assertion failed{': ' + message if message else ''}: expected {expected}, got {actual}")
    for i in range(len(actual)):
        if actual[i] != expected[i]:
            raise AssertionError(f"Array assertion failed{': ' + message if message else ''}: expected {expected}, got {actual}")
`,
  lua: `-- Function test setup code
-- Define test helper functions and setup here
function assertEquals(actual, expected, message)
    message = message or ''
    if actual ~= expected then
        error("Assertion failed" .. (message ~= '' and ': ' .. message or '') .. ": expected " .. tostring(expected) .. ", got " .. tostring(actual))
    end
end

function assertArrayEquals(actual, expected, message)
    message = message or ''
    if type(actual) ~= "table" or type(expected) ~= "table" or #actual ~= #expected then
        error("Array assertion failed" .. (message ~= '' and ': ' .. message or '') .. ": expected " .. tostring(expected) .. ", got " .. tostring(actual))
    end
    for i = 1, #actual do
        if actual[i] ~= expected[i] then
            error("Array assertion failed" .. (message ~= '' and ': ' .. message or '') .. ": expected " .. tostring(expected) .. ", got " .. tostring(actual))
        end
    end
end
`,
  c: `// Function test setup code
// Define test helper functions and setup here
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

void assert_equals_int(int actual, int expected, const char* message) {
    if (actual != expected) {
        printf("Assertion failed%s: expected %d, got %d\\n", 
               message ? message : "", expected, actual);
        exit(1);
    }
}

void assert_equals_str(const char* actual, const char* expected, const char* message) {
    if (strcmp(actual, expected) != 0) {
        printf("Assertion failed%s: expected '%s', got '%s'\\n", 
               message ? message : "", expected, actual);
        exit(1);
    }
}
`,
  cpp: `// Function test setup code
// Define test helper functions and setup here
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

template<typename T>
void assertEquals(const T& actual, const T& expected, const std::string& message = "") {
    if (actual != expected) {
        throw std::runtime_error("Assertion failed" + (message.empty() ? "" : ": " + message) + 
                                ": expected " + std::to_string(expected) + ", got " + std::to_string(actual));
    }
}

template<typename T>
void assertArrayEquals(const std::vector<T>& actual, const std::vector<T>& expected, const std::string& message = "") {
    if (actual.size() != expected.size()) {
        throw std::runtime_error("Array size mismatch" + (message.empty() ? "" : ": " + message));
    }
    for (size_t i = 0; i < actual.size(); ++i) {
        if (actual[i] != expected[i]) {
            throw std::runtime_error("Array assertion failed" + (message.empty() ? "" : ": " + message));
        }
    }
}
`,
  h: `// Function test setup code
// Define test helper functions and setup here
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

void assert_equals_int(int actual, int expected, const char* message);
void assert_equals_str(const char* actual, const char* expected, const char* message);
`,
  hpp: `// Function test setup code
// Define test helper functions and setup here
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

template<typename T>
void assertEquals(const T& actual, const T& expected, const std::string& message = "");

template<typename T>
void assertArrayEquals(const std::vector<T>& actual, const std::vector<T>& expected, const std::string& message = "");
`,
}

export const DEFAULT_SECOND_CODE_TEMPLATES: Record<ProgrammingLanguage, string> = {
  javascript: `// Function test validation code
// Add your test cases here using the helper functions
try {
  // Example test case:
  // const result = yourFunction(input);
  // assertEquals(result, expectedValue, "Test case description");
  
  console.log("All function tests passed!");
} catch (error) {
  console.log("Test failed: " + error.message);
}
`,
  typescript: `// Function test validation code
// Add your test cases here using the helper functions
try {
  // Example test case:
  // const result: any = yourFunction(input);
  // assertEquals(result, expectedValue, "Test case description");
  
  console.log("All function tests passed!");
} catch (error) {
  console.log("Test failed: " + (error as Error).message);
}
`,
  python: `# Function test validation code
# Add your test cases here using the helper functions
try:
    # Example test case:
    # result = your_function(input)
    # assert_equals(result, expected_value, "Test case description")
    
    print("All function tests passed!")
except Exception as error:
    print(f"Test failed: {error}")
`,
  lua: `-- Function test validation code
-- Add your test cases here using the helper functions
local success, error = pcall(function()
    -- Example test case:
    -- local result = yourFunction(input)
    -- assertEquals(result, expectedValue, "Test case description")
    
    print("All function tests passed!")
end)

if not success then
    print("Test failed: " .. tostring(error))
end
`,
  c: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    // Example test case:
    // int result = your_function(input);
    // assert_equals_int(result, expected_value, ": Test case description");
    
    printf("All function tests passed!\\n");
    return 0;
}
`,
  cpp: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    try {
        // Example test case:
        // auto result = yourFunction(input);
        // assertEquals(result, expectedValue, "Test case description");
        
        std::cout << "All function tests passed!" << std::endl;
    } catch (const std::exception& e) {
        std::cout << "Test failed: " << e.what() << std::endl;
        return 1;
    }
    return 0;
}
`,
  h: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    // Example test case:
    // int result = your_function(input);
    // assert_equals_int(result, expected_value, ": Test case description");
    
    printf("All function tests passed!\\n");
    return 0;
}
`,
  hpp: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    try {
        // Example test case:
        // auto result = yourFunction(input);
        // assertEquals(result, expectedValue, "Test case description");
        
        std::cout << "All function tests passed!" << std::endl;
    } catch (const std::exception& e) {
        std::cout << "Test failed: " << e.what() << std::endl;
        return 1;
    }
    return 0;
}
`,
}

// Add the new function test templates as exports
export const FUNCTION_FIRST_CODE_TEMPLATES: Record<ProgrammingLanguage, string> = {
  javascript: `// Function test setup code
// Define test helper functions and setup here
function assertEquals(actual, expected, message = '') {
  if (actual !== expected) {
    throw new Error(\`Assertion failed \${message ? ': ' + message : ''}: expected \${expected}, got \${actual}\`);
  }
}

function assertArrayEquals(actual, expected, message = '') {
  if (!Array.isArray(actual) || !Array.isArray(expected) || actual.length !== expected.length) {
    throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
  }
  for (let i = 0; i < actual.length; i++) {
    if (actual[i] !== expected[i]) {
      throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
    }
  }
}
`,
  typescript: `// Function test setup code
// Define test helper functions and setup here
function assertEquals(actual: any, expected: any, message: string = ''): void {
  if (actual !== expected) {
    throw new Error(\`Assertion failed \${message ? ': ' + message : ''}: expected \${expected}, got \${actual}\`);
  }
}

function assertArrayEquals(actual: any[], expected: any[], message: string = ''): void {
  if (!Array.isArray(actual) || !Array.isArray(expected) || actual.length !== expected.length) {
    throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
  }
  for (let i = 0; i < actual.length; i++) {
    if (actual[i] !== expected[i]) {
      throw new Error(\`Array assertion failed \${message ? ': ' + message : ''}: expected \${JSON.stringify(expected)}, got \${JSON.stringify(actual)}\`);
    }
  }
}
`,
  python: `# Function test setup code
# Define test helper functions and setup here
def assert_equals(actual, expected, message=''):
    if actual != expected:
        raise AssertionError(f"Assertion failed{': ' + message if message else ''}: expected {expected}, got {actual}")

def assert_array_equals(actual, expected, message=''):
    if not isinstance(actual, list) or not isinstance(expected, list) or len(actual) != len(expected):
        raise AssertionError(f"Array assertion failed{': ' + message if message else ''}: expected {expected}, got {actual}")
    for i in range(len(actual)):
        if actual[i] != expected[i]:
            raise AssertionError(f"Array assertion failed{': ' + message if message else ''}: expected {expected}, got {actual}")
`,
  lua: `-- Function test setup code
-- Define test helper functions and setup here
function assertEquals(actual, expected, message)
    message = message or ''
    if actual ~= expected then
        error("Assertion failed" .. (message ~= '' and ': ' .. message or '') .. ": expected " .. tostring(expected) .. ", got " .. tostring(actual))
    end
end

function assertArrayEquals(actual, expected, message)
    message = message or ''
    if type(actual) ~= "table" or type(expected) ~= "table" or #actual ~= #expected then
        error("Array assertion failed" .. (message ~= '' and ': ' .. message or '') .. ": expected " .. tostring(expected) .. ", got " .. tostring(actual))
    end
    for i = 1, #actual do
        if actual[i] ~= expected[i] then
            error("Array assertion failed" .. (message ~= '' and ': ' .. message or '') .. ": expected " .. tostring(expected) .. ", got " .. tostring(actual))
        end
    end
end
`,
  c: `// Function test setup code
// Define test helper functions and setup here
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

void assert_equals_int(int actual, int expected, const char* message) {
    if (actual != expected) {
        printf("Assertion failed%s: expected %d, got %d\\n", 
               message ? message : "", expected, actual);
        exit(1);
    }
}

void assert_equals_str(const char* actual, const char* expected, const char* message) {
    if (strcmp(actual, expected) != 0) {
        printf("Assertion failed%s: expected '%s', got '%s'\\n", 
               message ? message : "", expected, actual);
        exit(1);
    }
}
`,
  cpp: `// Function test setup code
// Define test helper functions and setup here
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

template<typename T>
void assertEquals(const T& actual, const T& expected, const std::string& message = "") {
    if (actual != expected) {
        throw std::runtime_error("Assertion failed" + (message.empty() ? "" : ": " + message) + 
                                ": expected " + std::to_string(expected) + ", got " + std::to_string(actual));
    }
}

template<typename T>
void assertArrayEquals(const std::vector<T>& actual, const std::vector<T>& expected, const std::string& message = "") {
    if (actual.size() != expected.size()) {
        throw std::runtime_error("Array size mismatch" + (message.empty() ? "" : ": " + message));
    }
    for (size_t i = 0; i < actual.size(); ++i) {
        if (actual[i] != expected[i]) {
            throw std::runtime_error("Array assertion failed" + (message.empty() ? "" : ": " + message));
        }
    }
}
`,
  h: `// Function test setup code
// Define test helper functions and setup here
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

void assert_equals_int(int actual, int expected, const char* message);
void assert_equals_str(const char* actual, const char* expected, const char* message);
`,
  hpp: `// Function test setup code
// Define test helper functions and setup here
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

template<typename T>
void assertEquals(const T& actual, const T& expected, const std::string& message = "");

template<typename T>
void assertArrayEquals(const std::vector<T>& actual, const std::vector<T>& expected, const std::string& message = "");
`,
}

export const FUNCTION_SECOND_CODE_TEMPLATES: Record<ProgrammingLanguage, string> = {
  javascript: `// Function test validation code
// Add your test cases here using the helper functions
try {
  // Example test case:
  // const result = yourFunction(input);
  // assertEquals(result, expectedValue, "Test case description");
  
  console.log("All function tests passed!");
} catch (error) {
  console.log("Test failed: " + error.message);
}
`,
  typescript: `// Function test validation code
// Add your test cases here using the helper functions
try {
  // Example test case:
  // const result: any = yourFunction(input);
  // assertEquals(result, expectedValue, "Test case description");
  
  console.log("All function tests passed!");
} catch (error) {
  console.log("Test failed: " + (error as Error).message);
}
`,
  python: `# Function test validation code
# Add your test cases here using the helper functions
try:
    # Example test case:
    # result = your_function(input)
    # assert_equals(result, expected_value, "Test case description")
    
    print("All function tests passed!")
except Exception as error:
    print(f"Test failed: {error}")
`,
  lua: `-- Function test validation code
-- Add your test cases here using the helper functions
local success, error = pcall(function()
    -- Example test case:
    -- local result = yourFunction(input)
    -- assertEquals(result, expectedValue, "Test case description")
    
    print("All function tests passed!")
end)

if not success then
    print("Test failed: " .. tostring(error))
end
`,
  c: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    // Example test case:
    // int result = your_function(input);
    // assert_equals_int(result, expected_value, ": Test case description");
    
    printf("All function tests passed!\\n");
    return 0;
}
`,
  cpp: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    try {
        // Example test case:
        // auto result = yourFunction(input);
        // assertEquals(result, expectedValue, "Test case description");
        
        std::cout << "All function tests passed!" << std::endl;
    } catch (const std::exception& e) {
        std::cout << "Test failed: " << e.what() << std::endl;
        return 1;
    }
    return 0;
}
`,
  h: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    // Example test case:
    // int result = your_function(input);
    // assert_equals_int(result, expected_value, ": Test case description");
    
    printf("All function tests passed!\\n");
    return 0;
}
`,
  hpp: `// Function test validation code
// Add your test cases here using the helper functions
int main() {
    try {
        // Example test case:
        // auto result = yourFunction(input);
        // assertEquals(result, expectedValue, "Test case description");
        
        std::cout << "All function tests passed!" << std::endl;
    } catch (const std::exception& e) {
        std::cout << "Test failed: " << e.what() << std::endl;
        return 1;
    }
    return 0;
}
`,
}

// Keep the existing getExtensionForSelectedLanguage function as export
export const getExtensionForSelectedLanguage = (language: ProgrammingLanguage): string => {
  switch (language) {
    case "javascript":
      return ".js"
    case "typescript":
      return ".ts"
    case "python":
      return ".py"
    case "lua":
      return ".lua"
    case "c":
      return ".c"
    case "cpp":
      return ".cpp"
    case "h":
      return ".h"
    case "hpp":
      return ".hpp"
    default:
      return ".txt"
  }
}
