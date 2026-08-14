import { describe, expect, it } from "vitest";
import { LEXICAL_SURFACE_NODES } from "./nodes";

describe("LexicalSurface node schema", () => {
  it("registers each serialized node type once", () => {
    const types = LEXICAL_SURFACE_NODES.map((nodeClass) => nodeClass.getType());

    expect(new Set(types).size).toBe(types.length);
    expect(types).toContain("heading");
    expect(types).toContain("lexical-media");
    expect(types).toContain("lexical-mermaid");
    expect(types).toContain("lexical-vega-lite");
    expect(types).toContain("page");
  });
});
