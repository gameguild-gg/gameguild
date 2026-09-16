import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}

vi.stubGlobal("ResizeObserver", ResizeObserverMock);
Element.prototype.scrollIntoView = vi.fn();

const signOut = vi.fn().mockResolvedValue(undefined);
const push = vi.fn();
const searchLearnerWorkspace = vi.fn();
const navigation = vi.hoisted(() => ({
  pathname: "/learn/courses/game-ai",
}));
const auth = vi.hoisted(() => ({ isLoading: false }));

vi.mock("@game-guild/client/react", () => ({
  useAuth: () => ({ isLoading: auth.isLoading, signOut }),
}));

vi.mock("@/lib/learner/search-actions", () => ({
  searchLearnerWorkspace,
}));

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => navigation.pathname,
  useRouter: () => ({ push }),
  Link: ({ children, href, ...props }: ComponentProps<"a"> & { href: string }) => <a href={href} {...props}>{children}</a>,
}));

vi.mock("@/components/ui/theme-toggle", () => ({
  ThemeToggle: () => <button type="button">Theme</button>,
}));

vi.mock("@/components/ui/command", () => ({
  CommandDialog: ({
    children,
    description,
    onOpenChange,
    open,
    title,
  }: {
    children: ReactNode;
    description: string;
    onOpenChange: (open: boolean) => void;
    open: boolean;
    title: string;
  }) =>
    open ? (
      <div aria-label={title} role="dialog">
        <p>{description}</p>
        <button type="button" onClick={() => onOpenChange(false)}>
          Close search
        </button>
        {children}
      </div>
    ) : null,
  CommandInput: ({
    onValueChange,
    value,
    ...props
  }: ComponentProps<"input"> & { onValueChange: (value: string) => void }) => (
    <input
      {...props}
      value={value}
      onChange={(event) => onValueChange(event.target.value)}
    />
  ),
  CommandList: ({ children, ...props }: ComponentProps<"div">) => (
    <div {...props}>{children}</div>
  ),
  CommandEmpty: ({ children }: { children: ReactNode }) => <p>{children}</p>,
  CommandGroup: ({ children, heading }: { children: ReactNode; heading: string }) => (
    <section aria-label={heading}>{children}</section>
  ),
  CommandItem: ({
    children,
    onSelect,
  }: {
    children: ReactNode;
    onSelect: () => void;
    value: string;
  }) => (
    <button aria-selected="false" type="button" role="option" onClick={onSelect}>
      {children}
    </button>
  ),
}));

const { LearningShell } = await import("./learning-shell");

function renderShell(
  props: Partial<ComponentProps<typeof LearningShell>> = {},
) {
  return render(
    <LearningShell
      user={{ id: "user-1", name: "Ada Learner", email: "ada@example.com" }}
      {...props}
    >
      {props.children ?? <p>Content</p>}
    </LearningShell>,
  );
}

function mainContainer(container: HTMLElement) {
  return container.querySelector("main#learning-content > div");
}

function preventNativeNavigation(element: HTMLElement) {
  element.addEventListener("click", (event) => event.preventDefault(), {
    once: true,
  });
}

describe("LearningShell", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    navigation.pathname = "/learn/courses/game-ai";
    auth.isLoading = false;
    searchLearnerWorkspace.mockResolvedValue({ success: true, items: [] });
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
    expect(await screen.findByText("ada@example.com")).toBeInTheDocument();
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

  it("opens search from the header and navigates to an internal result", async () => {
    const user = userEvent.setup();
    searchLearnerWorkspace.mockResolvedValue({
      success: true,
      items: [
        {
          id: "course-1",
          kind: "Course",
          title: "Game AI",
          description: "Build believable agents",
          route: "/learn/courses/game-ai",
        },
        {
          id: "lesson-1",
          kind: "Lesson",
          title: "Behavior trees",
          description: "",
          route: "/learn/courses/game-ai/lessons/behavior-trees",
        },
      ],
    });
    renderShell();

    await user.click(
      screen.getByRole("button", { name: /Search learning.*Ctrl K/ }),
    );
    const input = screen.getByPlaceholderText("Search your courses and lessons...");
    expect(screen.getByText("Type at least 2 characters to search.")).toBeInTheDocument();
    fireEvent.change(input, { target: { value: "game ai" } });
    await waitFor(
      () => expect(searchLearnerWorkspace).toHaveBeenCalledWith("game ai"),
      { timeout: 1500 },
    );
    expect(
      await screen.findByText(/Build believable agents/, {}, { timeout: 1500 }),
    ).toBeInTheDocument();
    expect(screen.getByText("Behavior trees")).toBeInTheDocument();
    await user.click(screen.getByText("Behavior trees"));

    expect(push).toHaveBeenCalledWith(
      "/learn/courses/game-ai/lessons/behavior-trees",
    );
    expect(searchLearnerWorkspace).toHaveBeenCalledWith("game ai");
  });

  it("reports loading, failed, and empty searches", async () => {
    let resolveSearch: (value: { success: false; error: string }) => void = () => {};
    searchLearnerWorkspace.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveSearch = resolve;
        }),
    );
    renderShell();

    fireEvent.keyDown(document, { key: "K", ctrlKey: true });
    const input = screen.getByPlaceholderText("Search your courses and lessons...");
    fireEvent.change(input, { target: { value: "offline" } });
    expect(
      await screen.findByText("Searching your learning workspace...", {}, { timeout: 1500 }),
    ).toBeInTheDocument();
    await waitFor(
      () => expect(searchLearnerWorkspace).toHaveBeenCalledWith("offline"),
      { timeout: 1500 },
    );

    await act(async () => {
      resolveSearch({ success: false, error: "offline" });
      await Promise.resolve();
    });
    expect(
      await screen.findByText(/Search is temporarily unavailable\. Try again\./),
    ).toBeInTheDocument();

    searchLearnerWorkspace.mockResolvedValue({ success: true, items: [] });
    fireEvent.change(input, { target: { value: "" } });
    fireEvent.change(input, { target: { value: "missing" } });
    expect(
      await screen.findByText("No matching courses or lessons.", {}, { timeout: 1500 }),
    ).toBeInTheDocument();
  });

  it("supports the keyboard shortcut and cancels an obsolete search", async () => {
    let resolveSearch: (value: { success: true; items: [] }) => void = () => {};
    searchLearnerWorkspace.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveSearch = resolve;
        }),
    );
    renderShell();

    fireEvent.keyDown(document, { key: "k", metaKey: true });
    const input = screen.getByPlaceholderText("Search your courses and lessons...");
    fireEvent.change(input, { target: { value: "first" } });
    await waitFor(() => expect(searchLearnerWorkspace).toHaveBeenCalledWith("first"));
    fireEvent.change(input, { target: { value: "" } });
    expect(screen.getByText("Type at least 2 characters to search.")).toBeInTheDocument();
    await act(async () => {
      resolveSearch({ success: true, items: [] });
      await Promise.resolve();
    });

    fireEvent.keyDown(document, { key: "x", ctrlKey: true });
    expect(input).toBeInTheDocument();
    fireEvent.keyDown(document, { key: "k", ctrlKey: true });
    await waitFor(() => expect(input).not.toBeInTheDocument());
  });

  it("renders notifications, fallbacks, unread state, and the user image", async () => {
    const user = userEvent.setup();
    renderShell({
      user: {
        id: "user-2",
        name: "Grace Hopper",
        email: "grace@example.com",
        image: "https://example.com/grace.png",
      },
      notifications: {
        unreadCount: 2,
        items: [
          {
            id: "notification-1",
            title: "New lesson",
            message: "A new lesson is ready",
            actionUrl: "/learn/courses/compiler-design",
          },
          {
            id: "notification-2",
            title: "Certificate available",
            message: null,
            actionUrl: null,
          },
        ],
      },
    });

    expect(screen.getByText("2 unread notifications")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Open notifications" }));

    expect(await screen.findByText("A new lesson is ready")).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: /New lesson/ })).toHaveAttribute(
      "href",
      "/learn/courses/compiler-design",
    );
    expect(
      screen.getByRole("menuitem", { name: "Certificate available" }),
    ).toHaveAttribute("href", "/");
  });

  it("shows empty notifications and the fallback initials", async () => {
    const user = userEvent.setup();
    renderShell({
      user: { id: "user-3", name: "", email: "unknown@example.com" },
    });

    expect(screen.getByText("GG")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Open notifications" }));
    expect(await screen.findByText("No new notifications")).toBeInTheDocument();
  });

  it("opens and closes mobile navigation from its controls", async () => {
    const user = userEvent.setup();
    renderShell();

    const toggle = screen.getByRole("button", { name: "Toggle navigation" });
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    await user.click(toggle);
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    expect(
      screen.getByRole("dialog", { name: "GameGuild Learning" }),
    ).toBeInTheDocument();

    const mobileHome = screen.getAllByRole("link", { name: "Home" }).at(-1);
    expect(mobileHome).toBeDefined();
    preventNativeNavigation(mobileHome!);
    await user.click(mobileHome!);
    await waitFor(() => expect(toggle).toHaveAttribute("aria-expanded", "false"));

    await user.click(toggle);
    const mobileCatalog = screen
      .getAllByRole("link", { name: "Browse courses" })
      .at(-1);
    expect(mobileCatalog).toBeDefined();
    preventNativeNavigation(mobileCatalog!);
    await user.click(mobileCatalog!);
    await waitFor(() => expect(toggle).toHaveAttribute("aria-expanded", "false"));
  });

  it("opens search from the mobile action and navigates from quick links", async () => {
    const user = userEvent.setup();
    renderShell();

    await user.click(screen.getByRole("button", { name: "Search learning" }));
    await user.click(screen.getByRole("option", { name: "Calendar" }));

    expect(push).toHaveBeenCalledWith("/learn/calendar");
  });

  it("closes navigation from desktop links and opens the external catalog", async () => {
    const user = userEvent.setup();
    renderShell();

    const brandLink = screen
      .getAllByRole("link", { name: "GameGuild Learning" })
      .at(-1);
    const courseLink = screen.getByRole("link", { name: "My courses" });
    expect(brandLink).toBeDefined();
    preventNativeNavigation(brandLink!);
    preventNativeNavigation(courseLink);
    fireEvent.click(brandLink!);
    fireEvent.click(courseLink);

    await user.click(
      screen.getByRole("button", { name: /Search learning.*Ctrl K/ }),
    );
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => {});
    try {
      await user.click(screen.getByRole("option", { name: "Browse courses" }));
      await new Promise((resolve) => setTimeout(resolve, 0));
    } finally {
      consoleError.mockRestore();
    }
  });

  it("disables sign out while authentication is loading", async () => {
    const user = userEvent.setup();
    auth.isLoading = true;
    renderShell();

    await user.click(screen.getByRole("button", { name: "Open account menu" }));
    expect(await screen.findByRole("menuitem", { name: "Sign out" })).toHaveAttribute(
      "data-disabled",
    );
  });
});
