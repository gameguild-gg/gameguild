// @vitest-environment node

import { describe, expect, it } from "vitest";

describe("monaco-file-system server compatibility", () => {
  it("can be imported while rendering without a browser window", async () => {
    let importError: unknown;

    try {
      await import("./monaco-file-system");
    } catch (error) {
      importError = error;
    }

    expect(importError).toBeUndefined();
  });
});
