import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => {
  // Minimal stand-in for Next.js' ReadonlyHeaders; verifyTurnstileToken only
  // calls .get() on it.
  const headerStore = new Map<string, string>();
  return { headerStore };
});

vi.mock("next/headers", () => ({
  headers: vi.fn(async () => ({
    get: (name: string) => mocks.headerStore.get(name.toLowerCase()) ?? null,
  })),
}));

const originalSecret = process.env.TURNSTILE_SECRET_KEY;

const importVerify = async () => {
  const { verifyTurnstileToken } = await import("./turnstile");
  return verifyTurnstileToken;
};

// The always-fail TEST secret (CF docs): siteverify responds success:false for
// any token, so no network-dependent assertions are needed — fetch is always
// mocked in this suite.
const FAILING_TEST_SECRET = "2x0000000000000000000000000000000AA";

describe("verifyTurnstileToken", () => {
  beforeEach(() => {
    vi.resetModules();
    delete process.env.TURNSTILE_SECRET_KEY;
    mocks.headerStore.clear();
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    if (originalSecret === undefined) {
      delete process.env.TURNSTILE_SECRET_KEY;
    } else {
      process.env.TURNSTILE_SECRET_KEY = originalSecret;
    }
    vi.restoreAllMocks();
  });

  it("fails open as disabled without calling fetch when the secret is unset", async () => {
    const warnSpy = vi.spyOn(console, "warn").mockImplementation(() => {});
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    const verifyTurnstileToken = await importVerify();

    await expect(verifyTurnstileToken("any-token")).resolves.toEqual({
      success: true,
      reason: "disabled",
    });

    expect(fetchMock).not.toHaveBeenCalled();
    expect(warnSpy).toHaveBeenCalledWith("Turnstile secret not set; anti-bot disabled");
  });

  it("returns success:false when Cloudflare rejects the token", async () => {
    process.env.TURNSTILE_SECRET_KEY = FAILING_TEST_SECRET;
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        json: async () => ({ success: false, "error-codes": ["invalid-input-response"] }),
      }),
    );
    const verifyTurnstileToken = await importVerify();

    await expect(verifyTurnstileToken("some-token")).resolves.toEqual({
      success: false,
      reason: "verify-failed",
    });
  });

  it("fails open with verify-unavailable when the siteverify request errors", async () => {
    process.env.TURNSTILE_SECRET_KEY = FAILING_TEST_SECRET;
    const errorSpy = vi.spyOn(console, "error").mockImplementation(() => {});
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("network down")));
    const verifyTurnstileToken = await importVerify();

    await expect(verifyTurnstileToken("some-token")).resolves.toEqual({
      success: true,
      reason: "verify-unavailable",
    });

    expect(errorSpy).toHaveBeenCalled();
  });

  it("fails closed with missing-token and skips fetch when the token is absent", async () => {
    process.env.TURNSTILE_SECRET_KEY = FAILING_TEST_SECRET;
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    const verifyTurnstileToken = await importVerify();

    for (const token of [undefined, null, "", "x".repeat(2049)]) {
      await expect(verifyTurnstileToken(token)).resolves.toEqual({
        success: false,
        reason: "missing-token",
      });
    }

    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("forwards the cf-connecting-ip as remoteip when verification succeeds", async () => {
    process.env.TURNSTILE_SECRET_KEY = FAILING_TEST_SECRET;
    mocks.headerStore.set("cf-connecting-ip", "198.51.100.23");
    mocks.headerStore.set("cf-ray", "9f3aCacheRay-1");
    const fetchMock = vi.fn().mockResolvedValue({
      json: async () => ({ success: true }),
    });
    vi.stubGlobal("fetch", fetchMock);
    const verifyTurnstileToken = await importVerify();

    await expect(verifyTurnstileToken("valid-token")).resolves.toEqual({ success: true });

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("https://challenges.cloudflare.com/turnstile/v0/siteverify");
    expect(JSON.parse(init.body)).toEqual({
      secret: FAILING_TEST_SECRET,
      response: "valid-token",
      remoteip: "198.51.100.23",
    });
    expect(init.signal).toBeDefined();
  });
});
