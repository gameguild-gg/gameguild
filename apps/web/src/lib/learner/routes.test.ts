import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  getPathname: ({ href, locale }: { href: string; locale: string }) =>
    locale === "en-US" ? href : `/${locale}${href}`,
}));

const {
  createLearnerRoutes,
  getLearnerCourseContentHref,
  getLearnerSignInHref,
  normalizeLearnerPathname,
} = await import("./routes");

describe("learner routes", () => {
  it("uses App Router paths and delegates locale prefixes to next-intl", () => {
    const routes = createLearnerRoutes();

    expect(routes.home).toBe("/learn");
    expect(routes.courses).toBe("/learn/courses");
    expect(routes.course("game-ai")).toBe("/learn/courses/game-ai");
    expect(routes.content("game-ai")).toBe("/learn/courses/game-ai/content");
    expect(routes.lesson("game-ai", "intro")).toBe(
      "/learn/courses/game-ai/lessons/intro",
    );
    expect(routes.activities("game-ai")).toBe(
      "/learn/courses/game-ai/activities",
    );
    expect(routes.activity("game-ai", "quiz-1")).toBe(
      "/learn/courses/game-ai/activities/quiz-1",
    );
    expect(routes.calendar).toBe("/learn/calendar");
    expect(routes.grades).toBe("/learn/grades");
    expect(routes.certificates).toBe("/learn/certificates");
    expect(routes.tasks).toBe("/dashboard/tasks");
    expect(routes.catalog).toBe("/courses");
    expect(routes.community("game-ai")).toBe(
      "/learn/courses/game-ai/community",
    );
  });

  it("delegates non-default locale prefixes to next-intl", () => {
    const routes = createLearnerRoutes("pt-BR");

    expect(routes.course("game-ai")).toBe("/pt-BR/learn/courses/game-ai");
    expect(routes.activities("game-ai")).toBe(
      "/pt-BR/learn/courses/game-ai/activities",
    );
  });

  it("falls back to the default locale for an invalid route segment", () => {
    const routes = createLearnerRoutes("invalid-locale");

    expect(routes.course("game-ai")).toBe("/learn/courses/game-ai");
  });

  it("builds course content links inside the native learner workspace", () => {
    expect(getLearnerCourseContentHref("ai games/intro")).toBe(
      "/learn/courses/ai%20games%2Fintro/content",
    );
  });

  it("creates a local sign-in URL that returns to the exact learner route", () => {
    expect(
      getLearnerSignInHref({
        pathname: "/pt-BR/learn/courses/game-ai/lessons/intro",
        search: "mode=focus",
      }),
    ).toBe(
      "/sign-in?redirectTo=%2Fpt-BR%2Flearn%2Fcourses%2Fgame-ai%2Flessons%2Fintro%3Fmode%3Dfocus",
    );
  });

  it("normalizes locale-prefixed, unprefixed, and root learner paths", () => {
    expect(normalizeLearnerPathname("/pt-BR/learn/courses/game-ai")).toBe(
      "/learn/courses/game-ai",
    );
    expect(normalizeLearnerPathname("/learn/courses/game-ai")).toBe(
      "/learn/courses/game-ai",
    );
    expect(normalizeLearnerPathname("///")).toBe("/");
  });

  it("sanitizes redirect paths and optional search strings", () => {
    expect(
      getLearnerSignInHref({
        pathname: "//malicious.example",
        search: "?q=game",
      }),
    ).toBe("/sign-in?redirectTo=%2Flearn%3Fq%3Dgame");
    expect(getLearnerSignInHref({ pathname: "relative/path" })).toBe(
      "/sign-in?redirectTo=%2Flearn",
    );
    expect(getLearnerSignInHref({ pathname: "/learn" })).toBe(
      "/sign-in?redirectTo=%2Flearn",
    );
  });
});
