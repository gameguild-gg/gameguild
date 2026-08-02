import { describe, expect, it } from "vitest";

import {
  createSharedAuthCookieConfig,
  resolveAllowedAuthRedirect,
} from "./cross-domain-auth";

describe("cross-domain auth", () => {
  it("uses a secure shared cookie only when explicitly configured", () => {
    expect(
      createSharedAuthCookieConfig({
        authCookieDomain: ".gameguild.gg",
        nodeEnv: "production",
      }),
    ).toEqual({
      domain: ".gameguild.gg",
      httpOnly: true,
      name: "gameguild",
      path: "/",
      sameSite: "lax",
      secure: true,
    });

    expect(
      createSharedAuthCookieConfig({
        authCookieDomain: "",

        nodeEnv: "development",
      }),
    ).toEqual({
      domain: undefined,

      httpOnly: true,

      name: "gameguild",

      path: "/",

      sameSite: "lax",

      secure: false,
    });

    expect(
      createSharedAuthCookieConfig({
        authCookieDomain: ".gameguild.127.0.0.1.sslip.io",
        authCookieSecure: "false",
        nodeEnv: "production",
      }),
    ).toEqual({
      domain: ".gameguild.127.0.0.1.sslip.io",
      httpOnly: true,
      name: "gameguild",
      path: "/",
      sameSite: "lax",
      secure: false,
    });
  });

  it("allows local routes and the configured website and learning origins", () => {
    const options = {
      learningOrigin: "https://learning.gameguild.gg",
      webOrigin: "https://gameguild.gg",
    };

    expect(resolveAllowedAuthRedirect("/dashboard", options)).toBe(
      "/dashboard",
    );
    expect(
      resolveAllowedAuthRedirect(
        "https://learning.gameguild.gg/courses/game-ai/lessons/intro?mode=focus",
        options,
      ),
    ).toBe(
      "https://learning.gameguild.gg/courses/game-ai/lessons/intro?mode=focus",
    );
    expect(
      resolveAllowedAuthRedirect("https://gameguild.gg/courses", options),
    ).toBe("https://gameguild.gg/courses");
  });

  it("rejects protocol-relative, foreign, malformed, and credential-bearing redirects", () => {
    const options = {
      fallback: "/dashboard",
      learningOrigin: "https://learning.gameguild.gg",
      webOrigin: "https://gameguild.gg",
    };

    expect(resolveAllowedAuthRedirect("//evil.example/path", options)).toBe(
      "/dashboard",
    );
    expect(
      resolveAllowedAuthRedirect(
        ["/dashboard", "https://evil.example"],
        options,
      ),
    ).toBe("/dashboard");
    expect(
      resolveAllowedAuthRedirect("https://evil.example/path", options),
    ).toBe("/dashboard");
    expect(
      resolveAllowedAuthRedirect(
        "https://admin:secret@learning.gameguild.gg/path",
        options,
      ),
    ).toBe("/dashboard");
    expect(resolveAllowedAuthRedirect("not a url", options)).toBe("/dashboard");
  });
});
