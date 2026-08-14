import { describe, expect, it } from "vitest";
import blankTemplate from "./blank";
import { getAllTemplates } from "./template-loader";

describe("blank Vega-Lite template", () => {
  it("starts with an empty specification", () => {
    expect(blankTemplate).toMatchObject({
      id: "blank-chart",
      type: "custom",
      category: "starter",
      initialTitle: "",
      spec: {},
    });
  });

  it("is the first choice in the template catalog", async () => {
    const templates = await getAllTemplates();
    expect(templates[0]).toBe(blankTemplate);
  });
});
