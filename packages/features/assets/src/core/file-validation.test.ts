import { describe, expect, it } from "vitest";
import { validateAssetFile } from "./file-validation";

describe("validateAssetFile", () => {
  it("applies type, kind, and size rules without reading bytes", () => {
    const issues = validateAssetFile(
      { name: "payload.html", type: "text/html", size: 20 },
      { accept: "image/*", kinds: ["image"], maxSizeBytes: 10 },
    );
    expect(issues.map((issue) => issue.code)).toEqual(["too-large", "type", "kind"]);
  });
});
