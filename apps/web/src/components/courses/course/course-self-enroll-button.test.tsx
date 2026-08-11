import "@testing-library/jest-dom/vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { CourseSelfEnrollButton } from "./course-self-enroll-button";

const mocks = vi.hoisted(() => ({
  enrollInFreeCourse: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock("@/lib/courses/actions/enrollment.actions", () => ({
  enrollInFreeCourse: mocks.enrollInFreeCourse,
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mocks.push,
    refresh: mocks.refresh,
  }),
}));

describe("CourseSelfEnrollButton", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.enrollInFreeCourse.mockResolvedValue({
      success: true,
      message: "Enrollment complete. You can continue in the learning app now.",
      learningUrl: "/learn/courses/ai4games/content",
    });
  });

  it("leaves the enrollment state and opens the learning workspace after success", async () => {
    const user = userEvent.setup();

    render(<CourseSelfEnrollButton courseSlug="ai4games" />);

    await user.click(screen.getByRole("button", { name: "Enroll now" }));

    await waitFor(() => {
      expect(mocks.push).toHaveBeenCalledWith(
        "/learn/courses/ai4games/content",
      );
    });

    expect(mocks.refresh).not.toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Enroll now" })).toBeEnabled();
    });
  });

  it("recovers from a failed enrollment without leaving the button pending", async () => {
    mocks.enrollInFreeCourse.mockResolvedValue({
      success: false,
      message: "Enrollment is temporarily unavailable.",
    });
    const user = userEvent.setup();

    render(<CourseSelfEnrollButton courseSlug="ai4games" />);
    await user.click(screen.getByRole("button", { name: "Enroll now" }));

    expect(
      await screen.findByText("Enrollment is temporarily unavailable."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Enroll now" })).toBeEnabled();
    expect(mocks.push).not.toHaveBeenCalled();
  });
});
