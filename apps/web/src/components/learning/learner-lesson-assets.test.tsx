import type { AssetRepository } from "@game-guild/assets";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const repository = vi.hoisted(() => ({
  createObjectUrl: vi.fn().mockImplementation(async (uri: string) => ({
    url: `https://cdn.example.test/${uri.slice("asset://".length)}`,
    release: vi.fn(),
  })),
}));
const lessonMocks = vi.hoisted(() => ({ recordLessonEvent: vi.fn() }));

vi.mock("@/lib/learning/assets/learning-asset-repository", () => ({
  getLearningAssetRepository: () => repository as unknown as AssetRepository,
}));

vi.mock("@/lib/learner/lesson-interaction-actions", () => ({
  recordLessonEvent: lessonMocks.recordLessonEvent,
}));
vi.mock("./lexical-lesson-renderer", () => ({
  LexicalLessonRenderer: () => <p>This Lexical lesson has no published content.</p>,
}));

import {
  asRecord,
  LearnerLessonRenderer,
  learningImageSource,
  learningUrlTransform,
  textContent,
  videoSource,
} from "./learner-lesson-renderer";

const assetUri = "asset://aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

describe("LearnerLessonRenderer assets", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    repository.createObjectUrl.mockImplementation(async (uri: string) => ({
      url: `https://cdn.example.test/${uri.slice("asset://".length)}`,
      release: vi.fn(),
    }));
  });

  it("normalizes every supported text and video content shape", () => {
    expect(asRecord(null)).toBeNull();
    expect(asRecord(["not", "a", "record"])).toBeNull();
    expect(asRecord("not json")).toBeNull();
    expect(asRecord("null")).toBeNull();
    expect(asRecord("[]")).toBeNull();
    expect(asRecord('{"markdown":"Hello"}')).toEqual({ markdown: "Hello" });
    expect(asRecord({ text: "Hello" })).toEqual({ text: "Hello" });

    expect(textContent("Plain text")).toBe("Plain text");
    expect(textContent(null)).toBe("");
    expect(textContent({ markdown: "Markdown" })).toBe("Markdown");
    expect(textContent({ content: "Content" })).toBe("Content");
    expect(textContent({ text: "Text" })).toBe("Text");
    expect(textContent({ source: "Source" })).toBe("Source");
    expect(textContent({ source: 42 })).toBe("");

    expect(videoSource({ videoUrl: "video-url", url: "url", src: "src" })).toBe("video-url");
    expect(videoSource({ url: "url", src: "src" })).toBe("url");
    expect(videoSource({ src: "src" })).toBe("src");
    expect(videoSource("fallback.mp4")).toBe("fallback.mp4");
    expect(videoSource({})).toBe("");
    expect(learningImageSource(assetUri)).toBe(assetUri);
    expect(learningImageSource(new Blob(["image"]))).toBeUndefined();
    expect(learningImageSource(undefined)).toBeUndefined();
    expect(learningUrlTransform(assetUri)).toBe(assetUri);
    expect(learningUrlTransform("https://example.test/lesson")).toBe("https://example.test/lesson");
  });

  it("resolves Markdown assets with the authenticated learning repository", async () => {
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-1"
        format="Markdown"
        content={`![Lesson asset](${assetUri})`}
      />,
    );

    await waitFor(() => {
      expect(screen.getByRole("img", { name: "Lesson asset" })).toHaveAttribute(
        "src",
        "https://cdn.example.test/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      );
    });
    expect(consoleError).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("resolves a video asset without exposing asset:// to the media element", async () => {
    const release = vi.fn();
    repository.createObjectUrl.mockResolvedValueOnce({
      url: "https://cdn.example.test/video",
      release,
    });
    const { container, unmount } = render(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-1"
        format="Video"
        content={{ src: assetUri }}
      />,
    );

    await waitFor(() => {
      expect(container.querySelector("video")).toHaveAttribute(
        "src",
        "https://cdn.example.test/video",
      );
    });
    unmount();
    expect(release).toHaveBeenCalledOnce();
  });

  it("renders empty, loading, and unavailable lesson states", async () => {
    const { rerender } = render(
      <LearnerLessonRenderer courseId="course-1" itemId="lesson-1" content={null} />,
    );
    expect(screen.getByText("This lesson has no published content.")).toBeInTheDocument();

    rerender(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-1"
        format="RevealJs"
        content=" --- "
      />,
    );
    expect(screen.getByText("This presentation has no published slides.")).toBeInTheDocument();

    rerender(
      <LearnerLessonRenderer courseId="course-1" itemId="lesson-1" format="Video" content={{}} />,
    );
    expect(screen.getByText("This video lesson has no published media.")).toBeInTheDocument();

    let resolveAsset!: (value: { url: string; release: () => void }) => void;
    repository.createObjectUrl.mockImplementationOnce(
      () => new Promise((resolve) => { resolveAsset = resolve; }),
    );
    rerender(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-1"
        format="Video"
        content={{ src: assetUri }}
      />,
    );
    expect(await screen.findByLabelText("Loading video")).toBeInTheDocument();
    resolveAsset({ url: "https://cdn.example.test/video", release: vi.fn() });
    await screen.findByLabelText("Video lesson");

    repository.createObjectUrl.mockRejectedValueOnce(new Error("Asset unavailable"));
    rerender(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-2"
        format="Video"
        content={{ src: "asset://eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee" }}
      />,
    );
    expect(await screen.findByText("This lesson media is unavailable.")).toBeInTheDocument();
  });

  it("navigates a Reveal presentation without leaving its slide bounds", async () => {
    const user = userEvent.setup();
    render(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-1"
        format="RevealJs"
        content={"# First\n\n---\n\n# Second"}
      />,
    );
    expect(screen.getByText("1 / 2")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Previous slide" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "Next slide" }));
    expect(screen.getByText("2 / 2")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Next slide" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "Previous slide" }));
    expect(screen.getByText("1 / 2")).toBeInTheDocument();
  });

  it("loads the Lexical renderer and preserves its empty published state", async () => {
    render(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-1"
        format="Lexical"
        content={null}
      />,
    );
    expect(await screen.findByText("This Lexical lesson has no published content.")).toBeInTheDocument();
  });

  it("records video lifecycle and throttled progress for the authenticated enrollment", async () => {
    const { container, rerender } = render(
      <LearnerLessonRenderer
        courseId="course-1"
        enrollmentId="enrollment-1"
        itemId="lesson-1"
        format="Video"
        content="https://video.example.test/lesson.mp4"
      />,
    );
    const video = container.querySelector("video")!;
    Object.defineProperty(video, "currentTime", { configurable: true, writable: true, value: 5 });
    Object.defineProperty(video, "duration", { configurable: true, value: 60 });
    fireEvent.play(video);
    fireEvent.timeUpdate(video);
    video.currentTime = 15;
    fireEvent.timeUpdate(video);
    video.currentTime = 20;
    fireEvent.timeUpdate(video);
    video.currentTime = 30;
    fireEvent.timeUpdate(video);
    fireEvent.pause(video);

    expect(lessonMocks.recordLessonEvent.mock.calls.map(([event]) => event.type)).toEqual([
      "Opened",
      "Progressed",
      "Progressed",
      "Paused",
    ]);
    expect(lessonMocks.recordLessonEvent).toHaveBeenNthCalledWith(
      4,
      expect.objectContaining({
        courseId: "course-1",
        enrollmentId: "enrollment-1",
        contentId: "lesson-1",
        durationSeconds: 60,
        progressPercentage: 50,
        idempotencyKey: expect.any(String),
      }),
    );

    Object.defineProperty(video, "duration", { configurable: true, value: Number.POSITIVE_INFINITY });
    fireEvent.pause(video);
    expect(lessonMocks.recordLessonEvent).toHaveBeenLastCalledWith(
      expect.objectContaining({ durationSeconds: undefined, progressPercentage: undefined }),
    );
    Object.defineProperty(video, "duration", { configurable: true, value: 0 });
    fireEvent.ended(video);
    expect(lessonMocks.recordLessonEvent).toHaveBeenLastCalledWith(
      expect.objectContaining({ type: "Completed", durationSeconds: 0, progressPercentage: undefined }),
    );

    lessonMocks.recordLessonEvent.mockClear();
    rerender(
      <LearnerLessonRenderer
        courseId="course-1"
        itemId="lesson-1"
        format="Video"
        content="https://video.example.test/lesson.mp4"
      />,
    );
    fireEvent.play(container.querySelector("video")!);
    expect(lessonMocks.recordLessonEvent).not.toHaveBeenCalled();
  });
});
