import { describe, expect, it, vi } from "vitest"

import { javascriptExecutor } from "./javascript-executor"
import type { ExecutionContext } from "./types"
import type { CodeFile, ProgrammingLanguage } from "@/components/block-content-editor/extras/source-code/types"

function createContext(): ExecutionContext {
  return {
    files: [] as CodeFile[],
    selectedLanguage: "javascript" as ProgrammingLanguage,
    addOutput: vi.fn(),
    setIsExecuting: vi.fn(),
  }
}

describe("javascriptExecutor.handleCommand", () => {
  it("evaluates an expression and reports its value", () => {
    const context = createContext()

    const handled = javascriptExecutor.handleCommand("2 + 3", context)

    expect(handled).toBe(true)
    expect(context.addOutput).toHaveBeenCalledWith("5")
  })

  it("evaluates an expression using globals such as Math", () => {
    const context = createContext()

    const handled = javascriptExecutor.handleCommand("Math.max(1, 7)", context)

    expect(handled).toBe(true)
    expect(context.addOutput).toHaveBeenCalledWith("7")
  })

  it("falls back to statement evaluation and reports undefined for statements", () => {
    const context = createContext()

    const handled = javascriptExecutor.handleCommand("let x = 5", context)

    expect(handled).toBe(true)
    expect(context.addOutput).toHaveBeenCalledWith("undefined")
  })

  it("reports an error for invalid input instead of throwing", () => {
    const context = createContext()

    const handled = javascriptExecutor.handleCommand("}", context)

    expect(handled).toBe(true)
    expect(context.addOutput).toHaveBeenCalledWith(expect.stringMatching(/^Error:/))
  })

  it("declines console.log commands so they run through the terminal path", () => {
    const context = createContext()

    const handled = javascriptExecutor.handleCommand('console.log("hi")', context)

    expect(handled).toBe(false)
    expect(context.addOutput).not.toHaveBeenCalled()
  })
})
