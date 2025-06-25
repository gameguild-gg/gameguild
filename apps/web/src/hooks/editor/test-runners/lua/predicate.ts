import type { TestCase, TestResult } from "../types"

// Convert JavaScript predicate to Lua
function convertPredicateToLua(predicate: string): string {
  // Replace JavaScript syntax with Lua syntax
  return predicate
    .replace(/===|==/g, "==")
    .replace(/!==|!=/g, "~=")
    .replace(/&&/g, "and")
    .replace(/\|\|/g, "or")
    .replace(/!/g, "not ")
    .replace(/true/g, "true")
    .replace(/false/g, "false")
    .replace(/null/g, "nil")
    .replace(/undefined/g, "nil")
    .replace(/\./g, ".")
    .replace(/\[(\d+)\]/g, "[$1+1]") // Lua arrays are 1-indexed
    .replace(/function\s*$$(.*?)$$\s*{(.*?)}/gs, "function($1) $2 end")
    .replace(/=>/g, "function(result) return ")
    .replace(/;/g, "")
}

export async function runPredicateTests(
  code: string,
  tests: TestCase[],
  executeLua: (code: string) => Promise<string>,
): Promise<TestResult[]> {
  // Extract the main function name from the Lua code
  const functionNameMatch = code.match(/function\s+(\w+)\s*\(/)
  const functionName = functionNameMatch ? functionNameMatch[1] : "solution"

  // Create a Lua test harness that runs all tests and reports results
  const testHarness = `
${code}

-- Test harness
local json = require("json")
local tests = ${JSON.stringify(tests)
    .replace(/\[/g, "{")
    .replace(/\]/g, "}")
    .replace(/"([^"]+)":/g, "$1=")
    .replace(/"predicate": "(.*?)"/g, (match, predicate) => {
      return `predicate = "${predicate.replace(/\\/g, "\\\\").replace(/"/g, '\\"')}"`
    })}

local results = {}

for i, test in ipairs(tests) do
  local success, result = pcall(function()
    -- Convert args to Lua format
    local args = {}
    for j, arg in ipairs(test.args) do
      args[j] = arg
    end
    
    -- Call the function with the arguments
    local result = ${functionName}(table.unpack(args))
    
    -- Convert the predicate to Lua and evaluate it
    local predicateStr = test.predicate
    local luaPredicate = ${JSON.stringify(tests.map((t) => convertPredicateToLua(t.predicate || "")))}[i]
    
    -- Create a function from the predicate
    local predicateFunc = load("return function(result) return " .. luaPredicate .. " end")()
    
    -- Evaluate the predicate
    local passed = predicateFunc(result)
    
    -- Format the result
    return {
      id = test.id,
      passed = passed,
      actual = result,
      predicate = predicateStr
    }
  end)
  
  if success then
    table.insert(results, result)
  else
    table.insert(results, {
      id = test.id,
      passed = false,
      error = result
    })
  end
end

-- Output the results as JSON
print("TEST_RESULTS_START")
print(json.encode(results))
print("TEST_RESULTS_END")
`

  try {
    // Execute the test harness
    const output = await executeLua(testHarness)

    // Extract the test results from the output
    const resultsMatch = output.match(/TEST_RESULTS_START\n(.*?)\nTEST_RESULTS_END/s)

    if (resultsMatch) {
      try {
        const results = JSON.parse(resultsMatch[1])

        // Convert the results to the expected format
        return results.map((result: any, index: number) => {
          if (result.error) {
            return {
              passed: false,
              actual: `Error: ${result.error}`,
              expected: `Predicate: ${tests[index].predicate}`,
            }
          }

          return {
            passed: result.passed,
            actual: `Test #${index + 1} returned: ${JSON.stringify(result.actual)}`,
            expected: `Predicate: ${result.predicate}`,
          }
        })
      } catch (error) {
        return [
          {
            passed: false,
            actual: `Error parsing test results: ${error.message}`,
            expected: "Valid test results",
          },
        ]
      }
    }

    return [
      {
        passed: false,
        actual: "Could not find test results in output",
        expected: "Test results in output",
      },
    ]
  } catch (error) {
    return [
      {
        passed: false,
        actual: `Error executing tests: ${error.message}`,
        expected: "Successful test execution",
      },
    ]
  }
}
