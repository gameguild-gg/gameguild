import type { editor } from "monaco-editor";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CppSyntaxHighlighter } from "./cpp-syntax-highlighter";
import { CppTypeChecker } from "./cpp-type-checker";

type Monaco = typeof import("monaco-editor");

afterEach(() => {
  vi.useRealTimers();
});

describe("C++ syntax registration lifecycle", () => {
  it("keeps the tokenizer active until disposal and releases it only once", () => {
    const dispose = vi.fn();
    const sdk = {
      languages: { setMonarchTokensProvider: vi.fn(() => ({ dispose })) },
    } as unknown as Monaco;
    const highlighter = new CppSyntaxHighlighter(sdk);

    highlighter.register();
    expect(dispose).not.toHaveBeenCalled();

    highlighter.dispose();
    expect(dispose).toHaveBeenCalledTimes(1);

    highlighter.dispose();
    expect(dispose).toHaveBeenCalledTimes(1);
  });

  it("can acquire and release a new tokenizer after a previous disposal", () => {
    const first = { dispose: vi.fn() };
    const second = { dispose: vi.fn() };
    const register = vi.fn().mockReturnValueOnce(first).mockReturnValueOnce(second);
    const highlighter = new CppSyntaxHighlighter({
      languages: { setMonarchTokensProvider: register },
    } as unknown as Monaco);

    highlighter.register();
    highlighter.dispose();
    highlighter.register();
    highlighter.dispose();

    expect(first.dispose).toHaveBeenCalledTimes(1);
    expect(second.dispose).toHaveBeenCalledTimes(1);
  });
});

function createChecker() {
  const model = {} as editor.ITextModel;
  const setModelMarkers = vi.fn();
  const sdk = {
    MarkerSeverity: { Error: 8, Warning: 4 },
    editor: { setModelMarkers },
  } as unknown as Monaco;
  const editorInstance = { getModel: () => model } as editor.IStandaloneCodeEditor;
  return { checker: new CppTypeChecker(sdk, editorInstance), model, setModelMarkers };
}

describe("C++ diagnostic scheduling lifecycle", () => {
  it("does not restore diagnostics after the checker is disposed", () => {
    vi.useFakeTimers();
    const { checker, model, setModelMarkers } = createChecker();

    checker.register("int answer = 42");
    checker.dispose();
    expect(setModelMarkers).toHaveBeenLastCalledWith(model, "cpp", []);
    setModelMarkers.mockClear();

    vi.advanceTimersByTime(100);
    expect(setModelMarkers).not.toHaveBeenCalled();
  });

  it("only publishes diagnostics for the latest queued source", () => {
    vi.useFakeTimers();
    const { checker, model, setModelMarkers } = createChecker();

    checker.register("int answer = 42");
    checker.register("int answer = 42;");
    vi.advanceTimersByTime(100);

    expect(setModelMarkers).toHaveBeenCalledTimes(1);
    expect(setModelMarkers).toHaveBeenCalledWith(model, "cpp", []);
    checker.dispose();
  });

  it("continues to publish diagnostics while the checker is active", () => {
    vi.useFakeTimers();
    const { checker, model, setModelMarkers } = createChecker();

    checker.register("int answer = 42");
    expect(setModelMarkers).not.toHaveBeenCalled();
    vi.advanceTimersByTime(100);

    expect(setModelMarkers).toHaveBeenCalledWith(model, "cpp", [
      expect.objectContaining({ severity: 4, startLineNumber: 1 }),
    ]);
    checker.dispose();
  });
});
