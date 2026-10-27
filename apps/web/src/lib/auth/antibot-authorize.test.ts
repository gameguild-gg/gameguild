import { afterEach, describe, expect, it, vi } from "vitest";

import { CredentialsSignInError, type ProviderResult } from "@game-guild/client";

import { withAntibotVerification } from "./antibot-authorize";

const mocks = vi.hoisted(() => ({
  verifyTurnstileToken: vi.fn(),
}));

vi.mock("@/lib/antibot/turnstile", () => ({
  verifyTurnstileToken: mocks.verifyTurnstileToken,
}));

const originalSecret = process.env.TURNSTILE_SECRET_KEY;

// A minimal password-like authorize: echoes back a fixed result so the wrapper
// tests can observe delegation. In auth.ts the wrapper wraps the real password
// authorize (CredentialsProvider default) inside
// createEmailCodeCredentialsAuthorize(createMagicLinkCredentialsAuthorize(
//   createWeb3CredentialsAuthorize(...)))
// — the wrapper is applied ONLY at that innermost layer, which is what exempts
// token-less flows (magic-link, MFA, web3) from the challenge.
const makeUnderlyingAuthorize = (result: ProviderResult | null) => vi.fn(async () => result);

afterEach(() => {
  if (originalSecret === undefined) {
    delete process.env.TURNSTILE_SECRET_KEY;
  } else {
    process.env.TURNSTILE_SECRET_KEY = originalSecret;
  }
  vi.clearAllMocks();
});

describe("withAntibotVerification", () => {
  it("delegates once with the original credentials object when the token verifies", async () => {
    process.env.TURNSTILE_SECRET_KEY = "test-secret";
    mocks.verifyTurnstileToken.mockResolvedValue({ success: true });
    const underlying = makeUnderlyingAuthorize({ user: { id: "u1" } } as ProviderResult);
    const authorize = withAntibotVerification(underlying);
    const credentials = { email: "a@b.c", password: "hunter2", turnstileToken: "tok" };

    await expect(authorize(credentials)).resolves.toEqual({ user: { id: "u1" } });

    expect(underlying).toHaveBeenCalledTimes(1);
    // The exact object is forwarded — the downstream authorize implementations
    // build fetch bodies from fixed fields only, so the extra turnstileToken
    // field is ignored and never serialized to the API (see antibot-authorize.ts).
    expect(underlying).toHaveBeenCalledWith(credentials, undefined);
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith("tok");
  });

  it("throws CredentialsSignInError without delegating when the token is missing", async () => {
    process.env.TURNSTILE_SECRET_KEY = "test-secret";
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: "missing-token" });
    const underlying = makeUnderlyingAuthorize({ user: { id: "u1" } } as ProviderResult);
    const authorize = withAntibotVerification(underlying);

    await expect(authorize({ email: "a@b.c", password: "hunter2" })).rejects.toThrow(
      new CredentialsSignInError("Bot verification failed"),
    );

    expect(underlying).not.toHaveBeenCalled();
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
  });

  it("throws CredentialsSignInError without delegating when verification fails", async () => {
    process.env.TURNSTILE_SECRET_KEY = "test-secret";
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: "verify-failed" });
    const underlying = makeUnderlyingAuthorize({ user: { id: "u1" } } as ProviderResult);
    const authorize = withAntibotVerification(underlying);

    await expect(authorize({ email: "a@b.c", password: "hunter2", turnstileToken: "bad" })).rejects.toThrow(
      CredentialsSignInError,
    );

    expect(underlying).not.toHaveBeenCalled();
  });

  it("delegates without any verification call when the secret is unset", async () => {
    delete process.env.TURNSTILE_SECRET_KEY;
    const underlying = makeUnderlyingAuthorize(null);
    const authorize = withAntibotVerification(underlying);
    const credentials = { email: "a@b.c", password: "hunter2" };

    await expect(authorize(credentials)).resolves.toBeNull();

    expect(mocks.verifyTurnstileToken).not.toHaveBeenCalled();
    expect(underlying).toHaveBeenCalledTimes(1);
    expect(underlying).toHaveBeenCalledWith(credentials, undefined);
  });

  it("delegates MFA completion without any verification call when the secret is set", async () => {
    // The default credentials provider also handles MFA completion
    // (mfaToken + method/code — see mfa-challenge-form.tsx). The 2FA page has
    // no widget, and the mfaToken is a server-issued first-factor bearer, so
    // the wrapper must delegate without a Turnstile check.
    process.env.TURNSTILE_SECRET_KEY = "test-secret";
    const underlying = makeUnderlyingAuthorize({ user: { id: "u1" } } as ProviderResult);
    const authorize = withAntibotVerification(underlying);
    const credentials = { mfaToken: "x", method: "Totp", code: "123" };

    await expect(authorize(credentials)).resolves.toEqual({ user: { id: "u1" } });

    expect(mocks.verifyTurnstileToken).not.toHaveBeenCalled();
    expect(underlying).toHaveBeenCalledTimes(1);
    expect(underlying).toHaveBeenCalledWith(credentials, undefined);
  });

  it("still gates password credentials without a turnstileToken when the secret is set", async () => {
    process.env.TURNSTILE_SECRET_KEY = "test-secret";
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: "missing-token" });
    const underlying = makeUnderlyingAuthorize({ user: { id: "u1" } } as ProviderResult);
    const authorize = withAntibotVerification(underlying);

    await expect(authorize({ email: "a@b.c", password: "hunter2" })).rejects.toThrow(
      CredentialsSignInError,
    );

    expect(underlying).not.toHaveBeenCalled();
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
  });

  it("does not exempt a password attempt with an appended bogus mfaToken (bypass closure)", async () => {
    // MFA completion never carries a password (fields: mfaToken + method +
    // code — see the client credentials provider). A payload with BOTH
    // password and mfaToken is a password attempt trying to smuggle the MFA
    // exemption; it must fall through to Turnstile verification.
    process.env.TURNSTILE_SECRET_KEY = "test-secret";
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: "missing-token" });
    const underlying = makeUnderlyingAuthorize({ user: { id: "u1" } } as ProviderResult);
    const authorize = withAntibotVerification(underlying);

    await expect(
      authorize({ email: "a@b.c", password: "hunter2", mfaToken: "bogus" }),
    ).rejects.toThrow(CredentialsSignInError);

    expect(underlying).not.toHaveBeenCalled();
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
  });

  it("does not treat a magicLinkToken-shaped payload as a Turnstile token", async () => {
    // Token-less flows are exempted by WHERE the wrapper is applied in auth.ts
    // (innermost password authorize only), not by field inspection. If a
    // magicLinkToken payload ever reaches the wrapped password layer, the
    // wrapper must not mistake that field for a Turnstile token: with no
    // turnstileToken present the verify call gets `undefined`, which
    // verifyTurnstileToken fails closed on when the secret is set.
    process.env.TURNSTILE_SECRET_KEY = "test-secret";
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: "missing-token" });
    const underlying = makeUnderlyingAuthorize({ user: { id: "u1" } } as ProviderResult);
    const authorize = withAntibotVerification(underlying);

    await expect(
      authorize({ magicLinkToken: "link-token", __apiUrl: "http://localhost:8080" }),
    ).rejects.toThrow(CredentialsSignInError);

    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
    expect(mocks.verifyTurnstileToken).not.toHaveBeenCalledWith("link-token");
    expect(underlying).not.toHaveBeenCalled();
  });
});
