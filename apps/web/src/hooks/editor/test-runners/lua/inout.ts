import type { TestCase, TestResult } from "../types"

export async function runInOutTests(
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
    .replace(/"([^"]+)":/g, "$1=")}

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
    
    -- Compare with expected result
    local expected = test.expectedReturn
    local passed = false
    
    if type(result) == "table" and type(expected) == "table" then
      -- Deep compare for tables
      local function deepEqual(a, b)
        if a == b then return true end
        if type(a) ~= "table" or type(b) ~= "table" then return false end
        
        for k, v in pairs(a) do
          if not deepEqual(v, b[k]) then return false end
        end
        
        for k, v in pairs(b) do
          if a[k] == nil then return false end
        end
        
        return true
      end
      
      passed = deepEqual(result, expected)
    else
      -- Simple compare for primitives
      passed = result == expected
    end
    
    -- Format the result
    return {
      id = test.id,
      passed = passed,
      actual = result,
      expected = expected
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
              expected: `Expected: ${JSON.stringify(tests[index].expectedReturn)}`,
            }
          }

          return {
            passed: result.passed,
            actual: `Test #${index + 1} returned: ${JSON.stringify(result.actual)}`,
            expected: `Expected: ${JSON.stringify(result.expected)}`,
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
