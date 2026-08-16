import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const signOut = vi.fn().mockResolvedValue(undefined);
const navigation = vi.hoisted(() => ({
  pathname: "/learn/courses/game-ai",
}));

vi.mock("@game-guild/client/react", () => ({
  useAuth: () => ({ isLoading: false, signOut }),
}));

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => navigation.pathname,
  useRouter: () => ({ push: vi.fn() }),
  Link: ({ children, href, ...props }: ComponentProps<"a"> & { href: string }) => <a href={href} {...props}>{children}</a>,
}));

vi.mock("@/components/ui/theme-toggle", () => ({
  ThemeToggle: () => <button type="button">Theme</button>,
}));

const { LearningShell } = await import("./learning-shell");

function renderShell() {
  return render(
    <LearningShell
      user={{ id: "user-1", name: "Ada Learner", email: "ada@example.com" }}
    >
      <p>Content</p>
    </LearningShell>,
  );
}

function mainContainer(container: HTMLElement) {
  return container.querySelector("main#learning-content > div");
}

describe("LearningShell", () => {
  beforeEach(() => {
    navigation.pathname = "/learn/courses/game-ai";
  });

  it("renders the coding assessment route in wide mode", () => {
    navigation.pathname = "/learn/courses/game-ai/activities/assessment-1";
    const { container } = renderShell();

    expect(mainContainer(container)).toHaveClass(
      "w-full",
      "px-4",
      "pt-4",
      "pb-6",
    );
    expect(mainContainer(container)).not.toHaveClass("max-w-[1600px]");
    expect(mainContainer(container)).not.toHaveClass("mx-auto");
  });

  it("keeps the capped container on non-assessment activity routes", () => {
    navigation.pathname = "/learn/courses/game-ai/activities/content-1";
    const { container } = renderShell();

    expect(mainContainer(container)).toHaveClass(
      "mx-auto",
      "w-full",
      "max-w-[1600px]",
      "p-4",
      "sm:p-6",
      "lg:p-8",
    );
  });

  it("keeps the capped container on other learner routes", () => {
    navigation.pathname = "/learn/other";
    const { container } = renderShell();

    expect(mainContainer(container)).toHaveClass(
      "mx-auto",
      "w-full",
      "max-w-[1600px]",
      "p-4",
      "sm:p-6",
      "lg:p-8",
    );
  });

  it("exposes the learner navigation with native App Router URLs", () => {
    render(
      <LearningShell
        user={{ id: "user-1", name: "Ada Learner", email: "ada@example.com" }}
      >
        <p>Learning workspace</p>
      </LearningShell>,
    );

    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute(
      "href",
      "/learn",
    );
    expect(screen.getByRole("link", { name: "My courses" })).toHaveAttribute(
      "href",
      "/learn/courses",
    );
    expect(screen.getByRole("link", { name: "My courses" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("link", { name: "Calendar" })).toHaveAttribute(
      "href",
      "/learn/calendar",
    );
    expect(screen.getByRole("link", { name: "Grades" })).toHaveAttribute(
      "href",
      "/learn/grades",
    );
    expect(screen.getByRole("link", { name: "Certificates" })).toHaveAttribute(
      "href",
      "/learn/certificates",
    );
    expect(
      screen.getByRole("link", { name: "Browse courses" }),
    ).toHaveAttribute("href", "https://gameguild.gg/courses");
    expect(screen.getByText("Learning workspace")).toBeInTheDocument();
  });

  it("keeps account identity and sign-out available", async () => {
    const user = userEvent.setup();
    render(
      <LearningShell
        user={{ id: "user-1", name: "Ada Learner", email: "ada@example.com" }}
      >
        <p>Content</p>
      </LearningShell>,
    );

    await user.click(screen.getByRole("button", { name: "Open account menu" }));
    expect(screen.getByText("ada@example.com")).toBeInTheDocument();
    await user.click(screen.getByRole("menuitem", { name: "Sign out" }));

    await waitFor(() =>
      expect(signOut).toHaveBeenCalledWith({
        redirectTo: "https://gameguild.gg/sign-in",
      }),
    );
  });
  it("marks the shell ready after hydration", async () => {
    const { container } = render(
      <LearningShell
        user={{ id: "user-1", name: "Ada Learner", email: "ada@example.com" }}
      >
        <p>Content</p>
      </LearningShell>,
    );

    await waitFor(() =>
      expect(container.firstElementChild).toHaveAttribute(
        "data-learning-ready",
        "true",
      ),
    );
  });
});
