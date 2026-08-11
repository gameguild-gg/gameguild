import { render, screen, waitFor } from "@testing-library/react";
import type { ComponentProps, ComponentType } from "react";
import { describe, expect, it, vi } from "vitest";

const navigation = vi.hoisted(() => ({
  pathname: "/pt-BR/learn/courses/game-ai/activities",
  replace: vi.fn(),
  searchParams: new URLSearchParams("view=open"),
}));

vi.mock("next/navigation", () => ({
  usePathname: () => navigation.pathname,
  useSearchParams: () => navigation.searchParams,
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    children,
    href,
    ...props
  }: ComponentProps<"a"> & { href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  useRouter: () => ({ replace: navigation.replace }),
}));

const { LearningAuthRedirect } = await import("./learning-auth-redirect");

describe("LearningAuthRedirect", () => {
  it("uses App Router navigation and preserves the current learner URL", async () => {
    const RedirectWithoutLegacyProp = LearningAuthRedirect as ComponentType<
      Record<string, never>
    >;

    render(<RedirectWithoutLegacyProp />);

    const expectedHref =
      "/sign-in?redirectTo=%2Fpt-BR%2Flearn%2Fcourses%2Fgame-ai%2Factivities%3Fview%3Dopen";

    await waitFor(() => {
      expect(navigation.replace).toHaveBeenCalledWith(expectedHref);
    });
    expect(
      screen.getByRole("link", { name: "Continue to sign in" }),
    ).toHaveAttribute("href", expectedHref);
  });
});
