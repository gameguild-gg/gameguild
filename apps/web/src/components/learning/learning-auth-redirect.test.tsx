import { render, screen, waitFor } from "@testing-library/react";
import type { ComponentProps, ComponentType } from "react";
import { describe, expect, it, vi } from "vitest";

const navigation = vi.hoisted(() => ({
  locale: "pt-BR",
  pathname: "/learn/courses/game-ai/activities",
  replace: vi.fn(),
  searchParams: new URLSearchParams("view=open"),
}));

vi.mock("next/navigation", () => ({
  useSearchParams: () => navigation.searchParams,
}));

vi.mock("next-intl", () => ({
  useLocale: () => navigation.locale,
}));

vi.mock("@/i18n/navigation", () => ({
  getPathname: ({ href, locale }: { href: string; locale: string }) =>
    locale === "en-US" ? href : `/${locale}${href}`,
  Link: ({
    children,
    href,
    ...props
  }: ComponentProps<"a"> & { href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  usePathname: () => navigation.pathname,
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

  it("keeps the default locale canonical without a URL prefix", async () => {
    navigation.locale = "en-US";

    render(<LearningAuthRedirect />);

    const expectedHref =
      "/sign-in?redirectTo=%2Flearn%2Fcourses%2Fgame-ai%2Factivities%3Fview%3Dopen";

    await waitFor(() => {
      expect(navigation.replace).toHaveBeenCalledWith(expectedHref);
    });
    expect(
      screen.getByRole("link", { name: "Continue to sign in" }),
    ).toHaveAttribute("href", expectedHref);
  });
});
