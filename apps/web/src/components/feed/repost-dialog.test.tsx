import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ repostPost: vi.fn() }));
vi.mock("@/lib/feed/actions", () => ({ repostPost: mocks.repostPost }));

import { RepostDialog } from "./repost-dialog";

function setViewport(mobile: boolean) {
  Object.defineProperty(window, "innerWidth", { configurable: true, value: mobile ? 500 : 1024 });
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    value: vi.fn().mockImplementation(() => ({
      matches: mobile,
      media: "(max-width: 767px)",
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  });
}

describe("RepostDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setViewport(false);
    mocks.repostPost.mockResolvedValue({ id: "repost-1", content: "", hasReposted: true, repostsCount: 4 });
  });
  afterEach(cleanup);

  it("submits optional commentary once and reconciles the authoritative repost count", async () => {
    let release!: (value: { id: string; content: string; hasReposted: boolean; repostsCount: number }) => void;
    mocks.repostPost.mockImplementationOnce(() => new Promise((resolve) => { release = resolve; }));
    render(<RepostDialog postId="post-1" initialReposted={false} initialCount={2} />);

    fireEvent.click(screen.getByRole("button", { name: "Repost" }));
    fireEvent.change(screen.getByLabelText("Repost commentary"), { target: { value: "Worth reading" } });
    const submit = screen.getByRole("button", { name: "Publish repost" });
    fireEvent.click(submit);
    fireEvent.click(submit);

    expect(mocks.repostPost).toHaveBeenCalledTimes(1);
    expect(mocks.repostPost).toHaveBeenCalledWith("post-1", "Worth reading");
    release({ id: "repost-1", content: "Worth reading", hasReposted: true, repostsCount: 7 });

    await waitFor(() => expect(screen.getByRole("button", { name: "Reposted" })).toHaveTextContent("7"));
    expect(screen.queryByRole("dialog", { name: "Repost this post" })).not.toBeInTheDocument();
  });

  it("rolls a rejected repost back and keeps the dialog retryable", async () => {
    mocks.repostPost.mockRejectedValueOnce(new Error("Repost unavailable"));
    render(<RepostDialog postId="post-1" initialReposted={false} initialCount={2} />);

    fireEvent.click(screen.getByRole("button", { name: "Repost" }));
    fireEvent.click(screen.getByRole("button", { name: "Publish repost" }));

    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Repost unavailable"));
    expect(screen.getByRole("dialog", { name: "Repost this post" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Publish repost" })).toBeEnabled();
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Repost" })).toHaveAttribute("aria-pressed", "false"));
  });

  it("uses a bottom drawer on mobile", async () => {
    setViewport(true);
    render(<RepostDialog postId="post-1" initialReposted={false} initialCount={0} />);

    fireEvent.click(screen.getByRole("button", { name: "Repost" }));

    await waitFor(() => expect(document.querySelector('[data-slot="drawer-content"]')).toBeInTheDocument());
  });
});
