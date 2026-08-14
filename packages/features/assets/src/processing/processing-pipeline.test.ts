import { describe, expect, it } from "vitest";
import { runAssetProcessingPipeline } from "./processing-pipeline";

describe("runAssetProcessingPipeline", () => {
  it("runs supported processors in order and accumulates warnings", async () => {
    const result = await runAssetProcessingPipeline(
      { blob: new Blob(["a"]), name: "a.txt", mimeType: "text/plain" },
      [{
        key: "append",
        supports: () => true,
        process: async (input) => ({
          ...input,
          blob: new Blob([await input.blob.text(), "b"]),
          warnings: ["changed"],
        }),
      }],
    );
    expect(await result.blob.text()).toBe("ab");
    expect(result.warnings).toEqual(["changed"]);
  });
});
