import assert from "node:assert/strict";
import test from "node:test";

import {
  assertSharedAuthCookie,
  classifyAppHttpFailure,
  learningViewports,
  resolveNodeHealthCheckUrl,
} from "./learning-browser-e2e-support.mjs";

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
