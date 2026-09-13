import "@testing-library/jest-dom/vitest";
import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createStory: vi.fn(),
  getSocialMediaStatus: vi.fn(),
  uploadSocialMediaWithProgress: vi.fn(),
}));

vi.mock("@/lib/feed/actions", () => ({
  createStory: mocks.createStory,
  getSocialMediaStatus: mocks.getSocialMediaStatus,
}));
vi.mock("@/lib/feed/social-media-upload", () => ({
  uploadSocialMediaWithProgress: mocks.uploadSocialMediaWithProgress,
}));

import { StoryComposer } from "./story-composer";

const image = new File(["pixels"], "story.png", { type: "image/png" });
const readyAsset = {
  assetReferenceId: "asset-1",
  deliveryUrl: "https://cdn.example/story.png",
  mimeType: "image/png",
  sizeBytes: image.size,
  state: "Ready" as const,
};
const publishedStory = {
  id: "story-1",
  assetReferenceId: "asset-1",
  authorId: "user-1",
  caption: "Launch day",
  createdAt: "2026-09-10T00:00:00Z",
  expiresAt: "2026-09-11T00:00:00Z",
  isViewed: true,
  mediaType: "image/png",
  mediaUrl: "https://cdn.example/story.png",
};

async function selectStory(file = image) {
  fireEvent.click(screen.getByRole("button", { name: "Add story" }));
  fireEvent.change(screen.getByLabelText("Add photo or video"), {
    target: { files: [file] },
  });
}

describe("StoryComposer", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:story-preview");
    vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => undefined);
    mocks.uploadSocialMediaWithProgress.mockResolvedValue(readyAsset);
    mocks.createStory.mockResolvedValue(publishedStory);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    cleanup();
  });

  it("publishes a validated upload with its caption and emits the authoritative story once", async () => {
    const onPublished = vi.fn();
    render(<StoryComposer userName="Me" onPublished={onPublished} />);
    await selectStory();
    fireEvent.change(screen.getByLabelText("Story caption"), {
      target: { value: " Launch day " },
    });

    fireEvent.click(screen.getByRole("button", { name: "Publish story" }));
    fireEvent.click(screen.getByRole("button", { name: "Publishing story" }));

    await waitFor(() =>
      expect(mocks.createStory).toHaveBeenCalledWith("asset-1", "Launch day"),
    );
    expect(mocks.uploadSocialMediaWithProgress).toHaveBeenCalledTimes(1);
    expect(mocks.createStory).toHaveBeenCalledTimes(1);
    expect(onPublished).toHaveBeenCalledTimes(1);
    expect(onPublished).toHaveBeenCalledWith(publishedStory);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("renders upload progress, supports cancellation, and keeps the draft retryable", async () => {
    mocks.uploadSocialMediaWithProgress.mockImplementation(
      (_file: File, options: { signal?: AbortSignal; onProgress?: (value: unknown) => void }) =>
        new Promise((_resolve, reject) => {
          options.onProgress?.({ loaded: 4, total: 8, percent: 50 });
          options.signal?.addEventListener("abort", () => reject(new Error("Media upload cancelled.")));
        }),
    );
    render(<StoryComposer userName="Me" onPublished={vi.fn()} />);
    await selectStory();
    fireEvent.click(screen.getByRole("button", { name: "Publish story" }));

    expect(await screen.findByRole("progressbar", { name: "Story upload progress" })).toHaveAttribute(
      "aria-valuenow",
      "50",
    );
    fireEvent.click(screen.getByRole("button", { name: "Cancel upload" }));

    expect(await screen.findByText("Media upload cancelled.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Retry story upload" })).toBeEnabled();
    expect(screen.getByAltText("Selected media preview")).toBeInTheDocument();
  });

  it("rejects failed processing and offers an explicit retry without creating a story", async () => {
    mocks.uploadSocialMediaWithProgress.mockResolvedValue({ ...readyAsset, state: "Processing" });
    mocks.getSocialMediaStatus.mockResolvedValue({ ...readyAsset, state: "Rejected" });
    render(<StoryComposer userName="Me" onPublished={vi.fn()} />);
    await selectStory();
    fireEvent.click(screen.getByRole("button", { name: "Publish story" }));

    expect(await screen.findByText(/could not be processed/i)).toBeInTheDocument();
    expect(mocks.createStory).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Retry story upload" })).toBeEnabled();
  });

  it("bounds processing polls and leaves timed-out media retryable", async () => {
    vi.useFakeTimers();
    mocks.uploadSocialMediaWithProgress.mockResolvedValue({ ...readyAsset, state: "Processing" });
    mocks.getSocialMediaStatus.mockResolvedValue({ ...readyAsset, state: "Processing" });
    render(<StoryComposer userName="Me" onPublished={vi.fn()} />);
    await selectStory();
    fireEvent.click(screen.getByRole("button", { name: "Publish story" }));

    await act(async () => vi.advanceTimersByTimeAsync(40_000));

    expect(mocks.getSocialMediaStatus).toHaveBeenCalledTimes(20);
    expect(mocks.createStory).not.toHaveBeenCalled();
    expect(screen.getByText(/timed out/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Retry story upload" })).toBeEnabled();
  });

  it("uses the existing media limits and lets the user remove a preview", async () => {
    render(<StoryComposer userName="Me" onPublished={vi.fn()} />);
    const oversized = new File([new Uint8Array(10 * 1024 * 1024 + 1)], "large.png", {
      type: "image/png",
    });
    await selectStory(oversized);
    expect(screen.getByRole("alert")).toHaveTextContent("Images can be up to 10 MB.");

    fireEvent.change(screen.getByLabelText("Add photo or video"), {
      target: { files: [image] },
    });
    expect(screen.getByAltText("Selected media preview")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Remove media" }));
    expect(screen.queryByAltText("Selected media preview")).not.toBeInTheDocument();
  });
});
