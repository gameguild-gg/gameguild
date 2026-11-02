// @vitest-environment node

import { beforeEach, describe, expect, it, vi } from "vitest";

const headersMock = vi.hoisted(() => vi.fn());

vi.mock("next/headers", () => ({
  headers: headersMock,
}));

import { getClientIp } from "./forwarded-headers";

describe("getClientIp", () => {
  beforeEach(() => {
    headersMock.mockReset();
  });

  it("returns the IP when cf-ray and cf-connecting-ip are present", async () => {
    headersMock.mockResolvedValue({
      get: (name: string) =>
        ({ "cf-ray": "9f3a-1", "cf-connecting-ip": "203.0.113.7" })[name] ?? null,
    });
    await expect(getClientIp()).resolves.toBe("203.0.113.7");
  });

  it("returns null when cf-ray is missing (not through Cloudflare)", async () => {
    headersMock.mockResolvedValue({
      get: (name: string) =>
        ({ "cf-connecting-ip": "203.0.113.7" })[name] ?? null,
    });
    await expect(getClientIp()).resolves.toBeNull();
  });

  it("returns null for a whitespace-only cf-ray", async () => {
    headersMock.mockResolvedValue({
      get: (name: string) =>
        ({ "cf-ray": "   ", "cf-connecting-ip": "203.0.113.7" })[name] ?? null,
    });
    await expect(getClientIp()).resolves.toBeNull();
  });

  it("returns null for a whitespace-only cf-connecting-ip", async () => {
    headersMock.mockResolvedValue({
      get: (name: string) =>
        ({ "cf-ray": "9f3a-1", "cf-connecting-ip": "   " })[name] ?? null,
    });
    await expect(getClientIp()).resolves.toBeNull();
  });
});
