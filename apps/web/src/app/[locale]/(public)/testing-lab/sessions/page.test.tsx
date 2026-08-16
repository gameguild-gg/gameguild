import { describe, expect, it, vi } from "vitest";

const { redirect } = vi.hoisted(() => ({
  redirect: vi.fn((path: string) => {
    throw new Error(`redirect:${path}`);
  }),
}));

vi.mock("next/navigation", () => ({ redirect }));

import TestingLabSessionsPage from "./page";

describe("Public Testing Lab sessions compatibility route", () => {
  it("redirects to the canonical events directory", async () => {
    await expect(TestingLabSessionsPage()).rejects.toThrow(
      "redirect:/testing-lab/events",
    );
  });
});
