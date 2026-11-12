import { describe, expect, it } from "vitest";
import {
  PROTECTED_ROUTE_PREFIXES,
  isProtectedPath,
  stripLocalePrefix,
} from "./protected-routes";

describe("stripLocalePrefix", () => {
  it("strips an explicit non-default locale prefix", () => {
    expect(stripLocalePrefix("/pt-BR/feed")).toBe("/feed");
    expect(stripLocalePrefix("/en-US/workspace/settings")).toBe("/workspace/settings");
  });

  it("collapses a bare locale prefix to the root", () => {
    expect(stripLocalePrefix("/pt-BR")).toBe("/");
    expect(stripLocalePrefix("/en-US")).toBe("/");
  });

  it("leaves unprefixed and unknown-prefix paths untouched", () => {
    expect(stripLocalePrefix("/feed")).toBe("/feed");
    expect(stripLocalePrefix("/english/feed")).toBe("/english/feed");
  });
});

describe("isProtectedPath", () => {
  it.each([...PROTECTED_ROUTE_PREFIXES])("treats /%s as protected", (prefix) => {
    expect(isProtectedPath(prefix)).toBe(true);
    expect(isProtectedPath(`${prefix}/deep/nested?tab=1`.split("?")[0])).toBe(true);
  });

  it("matches protected routes behind a locale prefix", () => {
    expect(isProtectedPath("/pt-BR/feed")).toBe(true);
    expect(isProtectedPath("/en-US/console/users")).toBe(true);
  });

  it("does not match partial-prefix lookalikes", () => {
    expect(isProtectedPath("/feedback")).toBe(false);
    expect(isProtectedPath("/workspaces-public")).toBe(false);
    expect(isProtectedPath("/mythology")).toBe(false);
  });

  it("treats public, auth, legal and learning roots as public", () => {
    for (const pathname of [
      "/",
      "/pricing",
      "/sign-in",
      "/sign-in/2fa",
      "/sign-up",
      "/forgot-password",
      "/legal/terms",
      "/block-content-editor",
      "/speedgrader/assessments/1",
      "/learn/courses/slug",
      "/blogs-public",
    ]) {
      expect(isProtectedPath(pathname)).toBe(false);
    }
  });
});
