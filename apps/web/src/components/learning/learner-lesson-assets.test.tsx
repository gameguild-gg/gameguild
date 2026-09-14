import type { AssetRepository } from "@game-guild/assets";
import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const repository = vi.hoisted(() => ({
  createObjectUrl: vi.fn().mockImplementation(async (uri: string) => ({
    url: `https://cdn.example.test/${uri.slice("asset://".length)}`,
    release: vi.fn(),
  })),
}));

vi.mock("@/lib/learning/assets/learning-asset-repository", () => ({
  getLearningAssetRepository: () => repository as unknown as AssetRepository,
}));

vi.mock("@/lib/learner/lesson-interaction-actions", () => ({
  recordLessonEvent: vi.fn(),
}));

import { LearnerLessonRenderer } from "./learner-lesson-renderer";

const assetUri = "asset://aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

describe("LearnerLessonRenderer assets", () => {
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
    const { container } = render(
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
        "https://cdn.example.test/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      );
    });
  });
});
