import { describe, expect, it, vi } from "vitest"

import { dispatchCodeExecution } from "./dispatch-code-execution"

describe("dispatchCodeExecution", () => {
  it("starts execution for the selected file without reporting a successful request as a failure", async () => {
    const completion = Promise.resolve()
    const execute = vi.fn<(fileId: string) => Promise<void>>().mockReturnValue(completion)
    const onFailure = vi.fn<() => void>()

    expect(dispatchCodeExecution(execute, "main.ts", onFailure)).toBeUndefined()
    expect(execute).toHaveBeenCalledOnce()
    expect(execute).toHaveBeenCalledWith("main.ts")
    await completion
    expect(onFailure).not.toHaveBeenCalled()
  })

  it("consumes an asynchronous failure and requests feedback without exposing exception details", async () => {
    const execute = vi.fn<(fileId: string) => Promise<void>>()
      .mockRejectedValue(new Error("private execution context"))
    const onFailure = vi.fn<() => void>()

    dispatchCodeExecution(execute, "main.ts", onFailure)

    await vi.waitFor(() => expect(onFailure).toHaveBeenCalledOnce())
    expect(onFailure).toHaveBeenCalledWith()
  })

  it("reports a synchronous failure through the same feedback boundary", () => {
    const execute = vi.fn<(fileId: string) => Promise<void>>().mockImplementation(() => {
      throw new Error("private execution context")
    })
    const onFailure = vi.fn<() => void>()

    expect(() => dispatchCodeExecution(execute, "main.ts", onFailure)).not.toThrow()
    expect(onFailure).toHaveBeenCalledOnce()
    expect(onFailure).toHaveBeenCalledWith()
  })
})
