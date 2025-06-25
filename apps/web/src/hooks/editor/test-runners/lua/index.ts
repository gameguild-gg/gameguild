import { runInOutTests } from "./inout"
import { runPredicateTests } from "./predicate"
import type { TestRunnerOptions } from "../types"

export const luaTestRunners = {
  inout: async (options: TestRunnerOptions) => {
    const { file, fileCases, setTestResults, fileId, setIsExecuting, addOutput } = options

    try {
      // Get the Lua executor from the options
      const executeLua = async (code: string): Promise<string> => {
        // Use the executor from options if available
        if (options.executeLua) {
          return options.executeLua(code)
        }

        // Otherwise, use a mock executor for testing
        addOutput("Warning: No Lua executor provided. Using mock executor.")
        return "TEST_RESULTS_START\n[]\nTEST_RESULTS_END"
      }

      // Filter inout tests
      const inoutTests = fileCases.filter((test) => test.type === "inout")

      if (inoutTests.length === 0) {
        addOutput("No input/output tests found.")
        setIsExecuting(false)
        return
      }

      addOutput(`Running ${inoutTests.length} input/output tests...`)

      // Run the tests
      const results = await runInOutTests(file.content, inoutTests, executeLua)

      // Update the test results
      setTestResults((prev: any) => ({
        ...prev,
        [fileId]: results,
      }))

      // Log the results
      const passedCount = results.filter((r) => r.passed).length
      addOutput(`Tests completed: ${passedCount}/${results.length} passed`)
    } catch (error) {
      console.error("Error running Lua inout tests:", error)
      addOutput(`Error running tests: ${error.message}`)
    } finally {
      setIsExecuting(false)
    }
  },

  predicate: async (options: TestRunnerOptions) => {
    const { file, fileCases, setTestResults, fileId, setIsExecuting, addOutput } = options

    try {
      // Get the Lua executor from the options
      const executeLua = async (code: string): Promise<string> => {
        // Use the executor from options if available
        if (options.executeLua) {
          return options.executeLua(code)
        }

        // Otherwise, use a mock executor for testing
        addOutput("Warning: No Lua executor provided. Using mock executor.")
        return "TEST_RESULTS_START\n[]\nTEST_RESULTS_END"
      }

      // Filter predicate tests
      const predicateTests = fileCases.filter((test) => test.type === "predicate")

      if (predicateTests.length === 0) {
        addOutput("No predicate tests found.")
        setIsExecuting(false)
        return
      }

      addOutput(`Running ${predicateTests.length} predicate tests...`)

      // Run the tests
      const results = await runPredicateTests(file.content, predicateTests, executeLua)

      // Update the test results
      setTestResults((prev: any) => ({
        ...prev,
        [fileId]: results,
      }))

      // Log the results
      const passedCount = results.filter((r) => r.passed).length
      addOutput(`Tests completed: ${passedCount}/${results.length} passed`)
    } catch (error) {
      console.error("Error running Lua predicate tests:", error)
      addOutput(`Error running tests: ${error.message}`)
    } finally {
      setIsExecuting(false)
    }
  },
}
