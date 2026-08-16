import { describe, expect, it, vi } from "vitest";

const { redirect } = vi.hoisted(() => ({
  redirect: vi.fn((path: string) => {
    throw new Error(`redirect:${path}`);
  }),
}));

vi.mock("next/navigation", () => ({ redirect }));

import CourseActivitiesPage from "./page";

describe("Legacy course activities route", () => {
  it("bridges to the locale-safe internal learner workspace", async () => {
    await expect(
      CourseActivitiesPage({
        params: Promise.resolve({ locale: "en-US", course: "ai4games" }),
      }),
    ).rejects.toThrow("redirect:/en-US/learn/courses/ai4games/activities");
  });

  it("preserves the locale and encodes the legacy course segment", async () => {
    await expect(
      CourseActivitiesPage({
        params: Promise.resolve({ locale: "pt-BR", course: "game design" }),
      }),
    ).rejects.toThrow("redirect:/pt-BR/learn/courses/game%20design/activities");
  });
});
