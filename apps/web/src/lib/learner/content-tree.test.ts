import { describe, expect, it } from "vitest";
import type { LearningCoursesProgramContent } from "@game-guild/client";
import { collectHiddenContentIds, flattenUniqueContent } from "./content-tree";

function content(
  overrides: Partial<LearningCoursesProgramContent> & { id: string },
): LearningCoursesProgramContent {
  return overrides;
}

describe("collectHiddenContentIds", () => {
  it("hides private items and their entire subtree", () => {
    const tree = [
      content({
        id: "module-1",
        visibility: "Public",
        children: [
          content({ id: "public-lesson", visibility: "Public" }),
          content({
            id: "private-module",
            visibility: "Private",
            children: [
              content({ id: "public-child-of-private", visibility: "Public" }),
            ],
          }),
        ],
      }),
    ];

    const hidden = collectHiddenContentIds(tree);

    expect([...hidden].sort()).toEqual(["private-module", "public-child-of-private"]);
  });

  it("keeps public trees empty", () => {
    const tree = [
      content({
        id: "module-1",
        visibility: "Public",
        children: [content({ id: "lesson-1", visibility: "Internal" })],
      }),
    ];

    expect(collectHiddenContentIds(tree).size).toBe(0);
  });
});

describe("flattenUniqueContent", () => {
  it("flattens nested children in sortOrder order", () => {
    const child = content({ id: "child-1", visibility: "Public", sortOrder: 1 });
    const tree = [
      content({
        id: "root-1",
        visibility: "Public",
        sortOrder: 2,
        children: [child],
      }),
      content({ id: "root-0", visibility: "Public", sortOrder: 0 }),
    ];

    expect(flattenUniqueContent(tree).map((item) => item.id)).toEqual([
      "root-0",
      "child-1",
      "root-1",
    ]);
  });
});
