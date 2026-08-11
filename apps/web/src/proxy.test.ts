// @vitest-environment node

import { NextRequest } from "next/server";
import { describe, expect, it } from "vitest";

import proxy from "./proxy";

describe("GameGuild internationalization proxy", () => {
  it("rewrites an unprefixed learner route to the default locale", () => {
    const response = proxy(
      new NextRequest(
        "https://gameguild.gg/learn/courses/game-ai/content?module=2",
        { headers: { "accept-language": "en-US" } },
      ),
    );

    expect(response.headers.get("x-middleware-rewrite")).toBe(
      "https://gameguild.gg/en-US/learn/courses/game-ai/content?module=2",
    );
  });

  it("preserves an explicitly selected non-default locale", () => {
    const response = proxy(
      new NextRequest(
        "https://gameguild.gg/pt-BR/learn/courses/game-ai/grades",
      ),
    );

    expect(response.headers.get("x-middleware-next")).toBe("1");
    expect(response.headers.get("x-middleware-rewrite")).toBeNull();
    expect(response.headers.get("location")).toBeNull();
  });
});
