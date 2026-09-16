import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ usePathname: vi.fn() }));

vi.mock("next/navigation", () => ({
  usePathname: mocks.usePathname,
}));

import { useLearningBase } from "./use-learning-base";

describe("useLearningBase", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it.each([
    ["/console", "/console/learning"],
    ["/console/learning/courses", "/console/learning"],
    ["/workspace/learning", "/workspace/learning"],
    [null, "/workspace/learning"],
  ])("maps %s to %s", (pathname, expected) => {
    mocks.usePathname.mockReturnValue(pathname);

    expect(useLearningBase()).toBe(expected);
  });
});
