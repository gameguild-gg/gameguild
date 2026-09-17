import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createReply: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock("@/lib/learner/activity-actions", () => ({
  createCourseDiscussionReply: mocks.createReply,
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ refresh: mocks.refresh }),
  Link: ({ children, href }: { children: ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

import { CourseDiscussionThread } from "./course-discussion-thread";

const discussion = {
  id: "discussion-1",
  courseId: "course-1",
  title: "How should we test the build?",
  content: "Share a focused testing approach.",
  isPinned: true,
  isResolved: true,
  viewCount: 12,
  createdAt: "2026-08-01T12:00:00Z",
};

describe("CourseDiscussionThread", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createReply.mockResolvedValue({ success: true });
  });

  it("renders the full thread and publishes a reply", async () => {
    render(
      <CourseDiscussionThread
        courseSlug="game-production"
        courseTitle="Game Production"
        discussion={discussion}
        replies={[
          {
            id: "reply-1",
            discussionId: "discussion-1",
            content: "Start with the onboarding loop.",
            isAcceptedAnswer: true,
            upvoteCount: 4,
            createdAt: "2026-08-02T12:00:00Z",
          },
        ]}
      />,
    );

    expect(screen.getByText("Pinned")).toBeInTheDocument();
    expect(screen.getByText("Resolved")).toBeInTheDocument();
    expect(screen.getByText("Accepted answer")).toBeInTheDocument();
    expect(
      screen.getByText("Start with the onboarding loop."),
    ).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Reply message"), {
      target: { value: "I will test that flow." },
    });
    fireEvent.click(screen.getByRole("button", { name: "Publish reply" }));

    await waitFor(() => expect(mocks.createReply).toHaveBeenCalledTimes(1));
    const submitted = mocks.createReply.mock.calls[0]?.[0] as FormData;
    expect(submitted.get("discussionId")).toBe("discussion-1");
    expect(submitted.get("content")).toBe("I will test that flow.");
    expect(await screen.findByText("Reply published")).toBeInTheDocument();
    expect(mocks.refresh).toHaveBeenCalledTimes(1);
  });

  it("keeps an API error visible for retry", async () => {
    mocks.createReply.mockResolvedValue({
      success: false,
      error: "Replies are temporarily unavailable.",
    });
    render(
      <CourseDiscussionThread
        courseSlug="game-production"
        courseTitle="Game Production"
        discussion={discussion}
        replies={[]}
      />,
    );

    fireEvent.change(screen.getByLabelText("Reply message"), {
      target: { value: "Retry this response." },
    });
    fireEvent.click(screen.getByRole("button", { name: "Publish reply" }));

    expect(
      await screen.findByText("Replies are temporarily unavailable."),
    ).toBeInTheDocument();
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it("renders missing discussion data and nested reply fallbacks", () => {
    render(
      <CourseDiscussionThread
        courseSlug="game-production"
        courseTitle="Game Production"
        discussion={{
          ...discussion,
          title: "",
          content: "",
          isPinned: false,
          isResolved: false,
          viewCount: undefined,
          createdAt: undefined,
        }}
        replies={[
          {
            id: "reply-2",
            discussionId: "discussion-1",
            parentReplyId: "reply-1",
            content: "A nested response.",
            isAcceptedAnswer: false,
            upvoteCount: undefined,
            createdAt: undefined,
          },
        ]}
      />,
    );

    expect(
      screen.getByRole("heading", { name: "Course discussion" }),
    ).toBeInTheDocument();
    expect(screen.getByText(/^Course conversation/)).toBeInTheDocument();
    expect(screen.getByText("0 views", { exact: false })).toBeInTheDocument();
    expect(
      screen.getByText("No discussion message was provided."),
    ).toBeInTheDocument();
    expect(
      screen.getByText("A nested response.").closest("article"),
    ).toHaveClass("ml-8");
    expect(screen.getByText("0 helpful", { exact: false })).toBeInTheDocument();
    expect(screen.queryByText("Pinned")).not.toBeInTheDocument();
    expect(screen.queryByText("Resolved")).not.toBeInTheDocument();
    expect(screen.queryByText("Accepted answer")).not.toBeInTheDocument();
  });

  it("shows pending state and the default error when the API omits a message", async () => {
    let finish: ((value: { success: false }) => void) | undefined;
    mocks.createReply.mockImplementation(
      () =>
        new Promise<{ success: false }>((resolve) => {
          finish = resolve;
        }),
    );
    render(
      <CourseDiscussionThread
        courseSlug="game-production"
        courseTitle="Game Production"
        discussion={discussion}
        replies={[]}
      />,
    );

    fireEvent.change(screen.getByLabelText("Reply message"), {
      target: { value: "Retry without an API message." },
    });
    fireEvent.click(screen.getByRole("button", { name: "Publish reply" }));

    expect(
      await screen.findByRole("button", { name: "Publishing..." }),
    ).toBeDisabled();
    finish?.({ success: false });

    expect(
      await screen.findByText("The reply could not be published."),
    ).toBeInTheDocument();
    expect(mocks.refresh).not.toHaveBeenCalled();
  });
});
