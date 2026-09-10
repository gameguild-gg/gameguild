import "@testing-library/jest-dom/vitest";
import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import * as React from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  deleteStory: vi.fn(),
  markStoryViewed: vi.fn(),
}));

vi.mock("@/lib/feed/actions", () => mocks);
vi.mock("next/image", () => ({
  default: () => <span data-testid="next-image" />,
}));

import { StoryViewer, type SocialStoryPreview } from "./story-viewer";

const stories: SocialStoryPreview[] = [
  {
    id: "story-1",
    assetReferenceId: "asset-1",
    authorId: "user-2",
    caption: "First build",
    createdAt: "2026-09-10T00:00:00Z",
    expiresAt: "2026-09-11T00:00:00Z",
    isViewed: false,
    mediaType: "image/png",
    mediaUrl: "https://cdn.example/story-1.png",
    authorName: "Ada",
    authorHandle: "ada",
    authorAvatarUrl: null,
    isOwn: false,
  },
  {
    id: "story-2",
    assetReferenceId: "asset-2",
    authorId: "user-1",
    caption: "My build",
    createdAt: "2026-09-10T01:00:00Z",
    expiresAt: "2026-09-11T01:00:00Z",
    isViewed: true,
    mediaType: "video/mp4",
    mediaUrl: "https://cdn.example/story-2.mp4",
    authorName: "Me",
    authorHandle: "me",
    authorAvatarUrl: null,
    isOwn: true,
  },
];

function StoryViewerHarness({
  initialId = "story-1",
  values = stories,
}: {
  initialId?: string;
  values?: SocialStoryPreview[];
}) {
  const [items, setItems] = React.useState(values);
  const [activeId, setActiveId] = React.useState<string | null>(initialId);
  return (
    <StoryViewer
      stories={items}
      activeStoryId={activeId}
      onActiveStoryChange={setActiveId}
      onStoryViewed={(id) =>
        setItems((current) =>
          current.map((story) =>
            story.id === id ? { ...story, isViewed: true } : story,
          ),
        )
      }
      onStoryDeleted={(id) =>
        setItems((current) => current.filter((story) => story.id !== id))
      }
    />
  );
}

describe("StoryViewer", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.markStoryViewed.mockResolvedValue({ storyId: "story-1", viewed: true });
    mocks.deleteStory.mockResolvedValue({ storyId: "story-2", deleted: true });
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      value: vi.fn().mockReturnValue({
        matches: false,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      }),
    });
    vi.spyOn(HTMLMediaElement.prototype, "play").mockResolvedValue(undefined);
    vi.spyOn(HTMLMediaElement.prototype, "pause").mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.useRealTimers();
    cleanup();
  });

  it("navigates with ArrowLeft/ArrowRight, closes with Escape, and records views without blocking", async () => {
    let releaseView!: () => void;
    mocks.markStoryViewed.mockReturnValue(
      new Promise((resolve) => {
        releaseView = () => resolve({ storyId: "story-1", viewed: true });
      }),
    );
    render(<StoryViewerHarness />);

    expect(screen.getByRole("heading", { name: "Ada" })).toBeInTheDocument();
    expect(screen.getByText("First build")).toBeInTheDocument();
    expect(mocks.markStoryViewed).toHaveBeenCalledWith("story-1");

    fireEvent.keyDown(screen.getByRole("dialog"), { key: "ArrowRight" });
    expect(screen.getByRole("heading", { name: "Me" })).toBeInTheDocument();
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "ArrowLeft" });
    expect(screen.getByRole("heading", { name: "Ada" })).toBeInTheDocument();

    fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    releaseView();
  });

  it("exposes semantic progress, pause/play, and pauses while the pointer is held", async () => {
    vi.useFakeTimers();
    render(<StoryViewerHarness />);

    expect(screen.getByRole("progressbar", { name: "Story progress" })).toHaveAttribute(
      "aria-valuemin",
      "0",
    );
    fireEvent.click(screen.getByRole("button", { name: "Pause story" }));
    await act(async () => vi.advanceTimersByTimeAsync(6_000));
    expect(screen.getByRole("heading", { name: "Ada" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Play story" }));
    const media = screen.getByTestId("story-media-surface");
    fireEvent.pointerDown(media);
    await act(async () => vi.advanceTimersByTimeAsync(6_000));
    expect(screen.getByRole("heading", { name: "Ada" })).toBeInTheDocument();
    fireEvent.pointerUp(media);
    await act(async () => vi.advanceTimersByTimeAsync(5_000));
    expect(screen.getByRole("heading", { name: "Me" })).toBeInTheDocument();
  });

  it("does not auto-advance when reduced motion is requested", async () => {
    vi.useFakeTimers();
    vi.mocked(window.matchMedia).mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    } as unknown as MediaQueryList);

    render(<StoryViewerHarness />);
    await act(async () => vi.advanceTimersByTimeAsync(10_000));

    expect(screen.getByRole("heading", { name: "Ada" })).toBeInTheDocument();
    expect(screen.getByText(/automatic advance is off/i)).toBeInTheDocument();
  });

  it("advances a video only after its ended event", () => {
    render(<StoryViewerHarness initialId="story-2" values={[stories[1]!, stories[0]!]} />);
    expect(screen.getByRole("heading", { name: "Me" })).toBeInTheDocument();

    fireEvent.ended(screen.getByLabelText("Story video by Me"));

    expect(screen.getByRole("heading", { name: "Ada" })).toBeInTheDocument();
  });

  it("does not auto-advance an ended video when reduced motion is requested", () => {
    vi.mocked(window.matchMedia).mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    } as unknown as MediaQueryList);
    render(<StoryViewerHarness initialId="story-2" values={[stories[1]!, stories[0]!]} />);

    fireEvent.ended(screen.getByLabelText("Story video by Me"));

    expect(screen.getByRole("heading", { name: "Me" })).toBeInTheDocument();
  });

  it("confirms owner deletion, serializes it, and keeps the story when deletion fails", async () => {
    let rejectDelete!: (reason: Error) => void;
    mocks.deleteStory.mockReturnValue(
      new Promise((_resolve, reject) => {
        rejectDelete = reject;
      }),
    );
    render(<StoryViewerHarness initialId="story-2" />);

    fireEvent.click(screen.getByRole("button", { name: "Delete story" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirm story deletion" }));
    fireEvent.click(screen.getByRole("button", { name: "Deleting story" }));
    expect(mocks.deleteStory).toHaveBeenCalledTimes(1);

    rejectDelete(new Error("Delete unavailable"));
    expect(await screen.findByText("Delete unavailable")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(screen.getByRole("heading", { name: "Me" })).toBeInTheDocument());
  });

  it("removes an owned story exactly once after confirmed deletion", async () => {
    render(<StoryViewerHarness initialId="story-2" />);
    fireEvent.click(screen.getByRole("button", { name: "Delete story" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirm story deletion" }));

    await waitFor(() => expect(mocks.deleteStory).toHaveBeenCalledWith("story-2"));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
});
