import { afterEach, describe, expect, it, vi } from "vitest";

type MonacoScope = typeof globalThis & {
  MonacoEnvironment?: {
    getWorker?: (workerId: string, label: string) => Worker | Promise<Worker>;
    getWorkerUrl?: (workerId: string, label: string) => string;
  };
};

describe("configureMonacoWorkers", () => {
  afterEach(() => {
    delete (globalThis as MonacoScope).MonacoEnvironment;
    vi.unstubAllGlobals();
    vi.resetModules();
  });

  it("does nothing during server rendering", async () => {
    vi.stubGlobal("window", undefined);
    const worker = vi.fn();
    vi.stubGlobal("Worker", worker);
    const { configureMonacoWorkers } =
      await import("./configure-monaco-workers");

    configureMonacoWorkers();

    expect((globalThis as MonacoScope).MonacoEnvironment).toBeUndefined();
    expect(worker).not.toHaveBeenCalled();
  });

  it("installs every Monaco language worker once and preserves existing settings", async () => {
    const constructed: Array<{
      url: URL;
      options: WorkerOptions;
    }> = [];
    const WorkerStub = vi.fn(function WorkerStub(
      this: { url: URL; options: WorkerOptions },
      url: URL,
      options: WorkerOptions,
    ) {
      this.url = url;
      this.options = options;
      constructed.push({ url, options });
    });
    vi.stubGlobal("Worker", WorkerStub);
    const getWorkerUrl = vi.fn(() => "legacy-worker.js");
    (globalThis as MonacoScope).MonacoEnvironment = { getWorkerUrl };
    const { configureMonacoWorkers } =
      await import("./configure-monaco-workers");

    configureMonacoWorkers();

    const environment = (globalThis as MonacoScope).MonacoEnvironment;
    expect(environment?.getWorkerUrl).toBe(getWorkerUrl);
    expect(environment?.getWorker).toBeTypeOf("function");

    const labels = [
      "json",
      "css",
      "scss",
      "less",
      "html",
      "handlebars",
      "razor",
      "typescript",
      "javascript",
      "plaintext",
    ];
    for (const label of labels) {
      environment?.getWorker?.("worker-id", label);
    }

    expect(constructed.map(({ options }) => options)).toEqual(
      labels.map((label) => ({ name: label, type: "module" })),
    );
    expect(constructed.map(({ url }) => url.pathname)).toEqual([
      expect.stringContaining("/language/json/json.worker.js"),
      ...Array.from({ length: 3 }, () =>
        expect.stringContaining("/language/css/css.worker.js"),
      ),
      ...Array.from({ length: 3 }, () =>
        expect.stringContaining("/language/html/html.worker.js"),
      ),
      ...Array.from({ length: 2 }, () =>
        expect.stringContaining("/language/typescript/ts.worker.js"),
      ),
      expect.stringContaining("/editor/editor.worker.js"),
    ]);

    const replacement = { getWorkerUrl: vi.fn(() => "replacement.js") };
    (globalThis as MonacoScope).MonacoEnvironment = replacement;
    configureMonacoWorkers();
    expect((globalThis as MonacoScope).MonacoEnvironment).toBe(replacement);
    expect(WorkerStub).toHaveBeenCalledTimes(labels.length);
  });
});
