import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  postReply: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));
vi.mock("next/cache", () => ({ revalidatePath: mocks.revalidatePath }));
vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningExperienceSocialRepliesModule: class {
      postApiSocialDiscussionsReplies = mocks.postReply;
    },
  },
}));

import { createCourseDiscussionReply } from "./activity-actions";

function replyForm(overrides: Record<string, string> = {}) {
  const form = new FormData();
  Object.entries({
    discussionId: "discussion-1",
    courseSlug: "visual-storytelling-by-maya",
    content: "I would test the onboarding first.",
    parentReplyId: "",
    ...overrides,
  }).forEach(([key, value]) => form.set(key, value));
  return form;
}

describe("learner discussion reply action", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue("access-token");
    mocks.createServerClient.mockReturnValue({ kind: "authenticated-client" });
    mocks.postReply.mockResolvedValue({ ok: true, data: { id: "reply-1" } });
  });

  it("validates the reply before authenticating", async () => {
    await expect(
      createCourseDiscussionReply(replyForm({ content: "   " })),
    ).resolves.toEqual({
      success: false,
      error: "A reply message is required.",
    });
    expect(mocks.getToken).not.toHaveBeenCalled();
    expect(mocks.postReply).not.toHaveBeenCalled();
  });

  it("requires an authenticated learner", async () => {
    mocks.getToken.mockResolvedValue(null);

    await expect(createCourseDiscussionReply(replyForm())).resolves.toEqual({
      success: false,
      error: "Your session expired. Sign in again.",
    });
    expect(mocks.postReply).not.toHaveBeenCalled();
  });

  it("publishes the reply and revalidates the list and thread routes", async () => {
    await expect(
      createCourseDiscussionReply(replyForm({ parentReplyId: "reply-parent" })),
    ).resolves.toEqual({ success: true });

    expect(mocks.postReply).toHaveBeenCalledWith("discussion-1", {
      discussionId: "discussion-1",
      content: "I would test the onboarding first.",
      parentReplyId: "reply-parent",
    });
    expect(mocks.revalidatePath).toHaveBeenNthCalledWith(
      1,
      "/learn/courses/visual-storytelling-by-maya/community",
    );
    expect(mocks.revalidatePath).toHaveBeenNthCalledWith(
      2,
      "/learn/courses/visual-storytelling-by-maya/community/discussion-1",
    );
  });

  it("returns the API detail and does not revalidate on failure", async () => {
    mocks.postReply.mockResolvedValue({
      ok: false,
      error: { detail: "Replies are closed." },
    });

    await expect(createCourseDiscussionReply(replyForm())).resolves.toEqual({
      success: false,
      error: "Replies are closed.",
    });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });
});
