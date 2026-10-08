import assert from "node:assert/strict";
import { test } from "node:test";
import {
  createMfaSignInContinuation,
  generateTotpCode,
} from "./sign-in-mfa-support.mjs";

const secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
const mfaToken = "x".repeat(43);
test("RFC 6238 SHA-1 vectors independently produce native six-digit TOTP", () => {
  for (const [seconds, expected] of [
    [59, "287082"],
    [1111111109, "081804"],
    [1111111111, "050471"],
    [1234567890, "005924"],
    [2000000000, "279037"],
    [20000000000, "353130"],
  ]) {
    assert.equal(generateTotpCode(secret, seconds * 1000), expected);
  }
});
test("malformed setup keys cannot become authenticator proofs", () => {
  for (const key of ["", "NOT BASE32!", undefined])
    assert.throws(() => generateTotpCode(key));
});
test("ordinary sign-in remains unchanged without requesting MFA", async () => {
  const response = { accessToken: "verified" };
  assert.equal(
    await createMfaSignInContinuation()(
      response,
      () => {
        throw new Error("unexpected MFA");
      },
      "account",
    ),
    response,
  );
});
test("enrollment and completion send only server-bound challenge data", async () => {
  const calls = [];
  const continuation = createMfaSignInContinuation({ now: () => 59000 });
  const response = await continuation(
    { requiresMfa: true, mfaToken, tenantId: "untrusted", userId: "untrusted" },
    async (action, body) => {
      calls.push({ action, body });
      return action === "enrollment"
        ? { success: true, secretKey: secret }
        : { accessToken: "verified" };
    },
    "account",
  );
  assert.deepEqual(response, { accessToken: "verified" });
  assert.deepEqual(calls, [
    { action: "enrollment", body: { mfaToken } },
    { action: "complete", body: { mfaToken, method: "Totp", code: "287082" } },
  ]);
});
test("second sign-in waits for a new counter instead of bypassing replay protection", async () => {
  let clock = 59000;
  const waits = [],
    codes = [];
  const continuation = createMfaSignInContinuation({
    secrets: new Map([["account", secret]]),
    now: () => clock,
    wait: async (ms) => {
      waits.push(ms);
      clock += ms;
    },
  });
  const send = async (action, body) => {
    assert.equal(action, "complete");
    codes.push(body.code);
    return { user: { id: "verified" } };
  };
  await continuation({ error: "MfaRequired", mfaToken }, send, "account");
  await continuation({ error: "MfaRequired", mfaToken }, send, "account");
  assert.deepEqual(waits, [1000]);
  assert.notEqual(codes[0], codes[1]);
});
test("denied enrollment or completion never returns an authenticated response", async () => {
  await assert.rejects(
    createMfaSignInContinuation()(
      { requiresMfa: true, mfaToken },
      async () => ({ success: false }),
      "account",
    ),
  );
  await assert.rejects(
    createMfaSignInContinuation({ secrets: new Map([["account", secret]]) })(
      { requiresMfa: true, mfaToken },
      async () => ({ error: "Denied" }),
      "account",
    ),
  );
});
