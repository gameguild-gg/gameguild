import assert from "node:assert/strict";
import test from "node:test";

import {
  assertSharedAuthCookie,
  classifyAppHttpFailure,
  learningViewports,
  resolveNodeHealthCheckUrl,
  waitForAuthenticatedNavigation,
} from "./learning-browser-e2e-support.mjs";

test("waits for cold-boot credential controls to become visible and enabled", async () => {
  const support = await import("./learning-browser-e2e-support.mjs");
  assert.equal(typeof support.waitForInteractiveCredentials, "function");

  const originalDocument = globalThis.document;
  const originalHtmlInputElement = globalThis.HTMLInputElement;
  const originalHtmlButtonElement = globalThis.HTMLButtonElement;
  let disabled = true;
  const locatorWaits = [];
  const email = {
    async waitFor(options) {
      locatorWaits.push({ label: "Email", options });
    },
  };
  const password = {
    async waitFor(options) {
      locatorWaits.push({ label: "Password", options });
    },
  };
  const submit = {
    async waitFor(options) {
      locatorWaits.push({ label: "submit", options });
    },
  };
  const page = {
    getByLabel(label) {
      return label === "Email" ? email : password;
    },
    getByRole(role, options) {
      assert.equal(role, "button");
      assert.deepEqual(options, { name: "Sign in", exact: true });
      return submit;
    },
    async waitForFunction(predicate, _arg, options) {
      assert.equal(options.timeout, 12_345);
      class FakeInput {
        constructor(nextDisabled) {
          this.disabled = nextDisabled;
        }
      }
      class FakeButton {
        constructor(nextDisabled) {
          this.disabled = nextDisabled;
        }
      }
      globalThis.HTMLInputElement = FakeInput;
      globalThis.HTMLButtonElement = FakeButton;
      globalThis.document = {
        getElementById(id) {
          return id === "email" || id === "password" ? new FakeInput(disabled) : null;
        },
        querySelector(selector) {
          return selector === 'button[type="submit"]' ? new FakeButton(disabled) : null;
        },
      };
      assert.equal(predicate(), false, "must not continue while controls are disabled");
      disabled = false;
      assert.equal(predicate(), true, "must continue once every control is enabled");
    },
  };

  try {
    const controls = await support.waitForInteractiveCredentials(page, { timeoutMs: 12_345 });
    assert.deepEqual(controls, { email, password, submit });
    assert.deepEqual(locatorWaits, [
      { label: "Email", options: { state: "visible", timeout: 12_345 } },
      { label: "Password", options: { state: "visible", timeout: 12_345 } },
      { label: "submit", options: { state: "visible", timeout: 12_345 } },
    ]);
  } finally {
    globalThis.document = originalDocument;
    globalThis.HTMLInputElement = originalHtmlInputElement;
    globalThis.HTMLButtonElement = originalHtmlButtonElement;
  }
});

test("waits long enough for a cold authenticated dashboard navigation", async () => {
  const calls = [];
  const page = {
    async waitForURL(predicate, options) {
      calls.push(options);
      assert.equal(predicate(new URL("http://localhost:3012/dashboard")), true);
      assert.equal(predicate(new URL("http://localhost:3012/sign-in")), false);
      assert.equal(predicate(new URL("http://localhost:3012/sign-up")), false);
    },
  };

  await waitForAuthenticatedNavigation(page, { timeoutMs: 123_456 });
  assert.deepEqual(calls, [{ timeout: 123_456 }]);
});

test("accepts the session cookie when a cold sign-in page does not redirect", async () => {
  const page = {
    async waitForURL() {
      throw new Error("the auth component kept the browser on /sign-in");
    },
    context() {
      return {
        async cookies() {
          return [
            {
              domain: "localhost",
              httpOnly: true,
              name: "gameguild.session-token",
              sameSite: "Lax",
              value: "opaque",
            },
          ];
        },
      };
    },
    async waitForTimeout() {},
  };

  await assert.doesNotReject(() =>
    waitForAuthenticatedNavigation(page, { timeoutMs: 1_000 }),
  );
});

test("classifies same-origin 404, server, and RSC failures", () => {
  const origins = [
    "http://gameguild.localhost:3011",
    "http://learning.gameguild.localhost:3011",
  ];

  assert.equal(
    classifyAppHttpFailure({
      origins,
      resourceType: "fetch",
      status: 404,
      url: "http://learning.gameguild.localhost:3011/courses/game-ai?_rsc=abc",
    }),
    "404 RSC fetch http://learning.gameguild.localhost:3011/courses/game-ai?_rsc=abc",
  );
  assert.equal(
    classifyAppHttpFailure({
      origins,
      resourceType: "document",
      status: 503,
      url: "http://gameguild.localhost:3011/sign-in",
    }),
    "503 document http://gameguild.localhost:3011/sign-in",
  );
  assert.equal(
    classifyAppHttpFailure({
      origins,
      resourceType: "fetch",
      status: 401,
      url: "http://gameguild.localhost:3011/api/auth/session",
    }),
    null,
  );
  assert.equal(
    classifyAppHttpFailure({
      origins,
      resourceType: "image",
      status: 404,
      url: "https://images.example.test/unavailable.png",
    }),
    null,
  );
});

test("requires an HttpOnly SameSite=Lax auth cookie on the shared parent domain", () => {
  const cookies = [
    {
      domain: ".gameguild.localhost",
      httpOnly: true,
      name: "gameguild.session-token",
      sameSite: "Lax",
      value: "encrypted-session",
    },
  ];

  assert.deepEqual(
    assertSharedAuthCookie(cookies, ".gameguild.localhost"),
    cookies,
  );
  assert.doesNotThrow(() =>
    assertSharedAuthCookie(
      [{ ...cookies[0], name: "__Secure-gameguild.session-token.1" }],
      ".gameguild.localhost",
    ),
  );
  assert.throws(
    () =>
      assertSharedAuthCookie(
        [{ ...cookies[0], domain: "gameguild.localhost" }],
        ".example.test",
      ),
    /not shared/,
  );
  assert.throws(
    () =>
      assertSharedAuthCookie(
        [{ ...cookies[0], httpOnly: false }],
        ".gameguild.localhost",
      ),
    /HttpOnly/,
  );
});

test("defines the required responsive browser matrix", () => {
  assert.deepEqual(
    learningViewports.map(({ width }) => width),
    [390, 768, 1440, 1920],
  );
});

test("uses an explicit loopback address for Node probes of local app hosts", () => {
  assert.equal(
    resolveNodeHealthCheckUrl("http://gameguild.localhost:3011/api/health"),
    "http://127.0.0.1:3011/api/health",
  );
  assert.equal(
    resolveNodeHealthCheckUrl(
      "http://learning.gameguild.localhost:3011/courses",
    ),
    "http://127.0.0.1:3011/courses",
  );
  assert.equal(
    resolveNodeHealthCheckUrl("https://learning.gameguild.gg/courses"),
    "https://learning.gameguild.gg/courses",
  );
});
