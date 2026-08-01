import { parseMermaidToExcalidraw } from "@excalidraw/mermaid-to-excalidraw";
import { describe, expect, it } from "vitest";

describe("Excalidraw Mermaid integration", () => {
  it("converts a Mermaid flowchart into Excalidraw element skeletons", async () => {
    Object.defineProperty(SVGElement.prototype, "getBBox", {
      configurable: true,
      value: () => ({ height: 16, width: 40, x: 0, y: 0 }),
    });

    const { elements } = await parseMermaidToExcalidraw("flowchart LR\n  start[Start] --> finish[Finish]");

    expect(elements).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          label: expect.objectContaining({ text: "Start" }),
          type: "rectangle",
        }),
        expect.objectContaining({
          label: expect.objectContaining({ text: "Finish" }),
          type: "rectangle",
        }),
        expect.objectContaining({ type: "arrow" }),
      ]),
    );
  }, 60_000);
});
