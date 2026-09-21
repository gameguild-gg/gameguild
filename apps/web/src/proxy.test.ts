// @vitest-environment node

import { NextRequest } from "next/server";
import { describe, expect, it } from "vitest";

import { routeRequest } from "./proxy";

describe("GameGuild internationalization proxy", () => {
  it("keeps the default locale internal for an unprefixed route", () => {
    const response = routeRequest(
      new NextRequest(
        "https://gameguild.gg/learn/courses/game-ai/content?module=2",
        { headers: { "accept-language": "en-US" } },
      ),
    );

    expect(response.headers.get("x-middleware-rewrite")).toBe(
      "https://gameguild.gg/en-US/learn/courses/game-ai/content?module=2",
    );
    expect(response.headers.get("location")).toBeNull();
  });

  it("removes an explicit default-locale prefix", () => {
    const response = routeRequest(
      new NextRequest("https://gameguild.gg/en-US/projects?view=grid"),
    );

    expect(response.headers.get("location")).toBe(
      "https://gameguild.gg/projects?view=grid",
    );
  });

  it("preserves an explicitly selected non-default locale", () => {
    const response = routeRequest(
      new NextRequest(
        "https://gameguild.gg/pt-BR/learn/courses/game-ai/grades",
      ),
    );

    expect(response.headers.get("x-middleware-next")).toBe("1");
    expect(response.headers.get("x-middleware-rewrite")).toBeNull();
    expect(response.headers.get("location")).toBeNull();
  });

  it("keeps the root on the public page so signed-in members get the /feed redirect", () => {
    const response = routeRequest(new NextRequest("https://gameguild.gg/"));

    expect(response.headers.get("x-middleware-rewrite")).toBe(
      "https://gameguild.gg/en-US",
    );
    expect(response.headers.get("location")).toBeNull();
  });

  it("serves the social feed at /feed through an internal default-locale rewrite", () => {
    const response = routeRequest(
      new NextRequest("https://gameguild.gg/feed?tab=following"),
    );

    expect(response.headers.get("x-middleware-rewrite")).toBe(
      "https://gameguild.gg/en-US/feed?tab=following",
    );
    expect(response.headers.get("location")).toBeNull();
  });

  it("canonicalizes the legacy social URL to /feed", () => {
    const response = routeRequest(
      new NextRequest("https://gameguild.gg/social?tab=playtests"),
    );

    expect(response.headers.get("location")).toBe(
      "https://gameguild.gg/feed?tab=playtests",
    );
  });
});
