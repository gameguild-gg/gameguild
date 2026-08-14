import { describe, expect, it } from "vitest";
import blankTemplate from "./blank";
import { getAllTemplates } from "./template-loader";

describe("blank Mermaid template", () => {
  it("starts with an empty document", () => {
    expect(blankTemplate).toMatchObject({
      id: "blank-diagram",
      type: "flowchart",
      category: "starter",
      code: "",
    });
  });

  it("is the first choice in the template catalog", async () => {
    const templates = await getAllTemplates();
    expect(templates[0]).toBe(blankTemplate);
  });
});
