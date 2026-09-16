import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ updateCourseReviewModeration: vi.fn() }));

vi.mock("@/lib/learning/actions", () => ({
  updateCourseReviewModeration: mocks.updateCourseReviewModeration,
}));

import { TestimonialsManager } from "./testimonials-manager";

describe("TestimonialsManager", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.updateCourseReviewModeration.mockResolvedValue({
      success: true,
      data: null,
    });
  });

  it("approves and features a review for the public storefront", async () => {
    const user = userEvent.setup();
    render(
      <TestimonialsManager
        courseId="course-1"
        testimonials={{
          total: 1,
          averageRating: 5,
          ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 0, 5: 1 },
          testimonials: [
            {
              id: "review-1",
              courseId: "course-1",
              studentId: "student-1",
              studentName: "Student One",
              rating: 5,
              title: "Excellent course",
              content: "Strong production workflow.",
              featured: false,
              approved: false,
              verified: true,
              helpful: 2,
              createdAt: "2026-07-10T00:00:00.000Z",
              updatedAt: "2026-07-10T00:00:00.000Z",
            },
          ],
        }}
      />,
    );

    await user.click(
      screen.getByRole("switch", { name: "Approve review Excellent course" }),
    );
    await waitFor(() =>
      expect(mocks.updateCourseReviewModeration).toHaveBeenCalledWith(
        "course-1",
        "review-1",
        true,
        false,
      ),
    );
    await user.click(
      screen.getByRole("switch", { name: "Feature review Excellent course" }),
    );
    await waitFor(() =>
      expect(mocks.updateCourseReviewModeration).toHaveBeenLastCalledWith(
        "course-1",
        "review-1",
        true,
        true,
      ),
    );

    expect(screen.getByText("Approved for storefront")).toBeInTheDocument();
    expect(screen.getAllByText("Featured")).toHaveLength(2);
  });

  it("keeps the previous state and shows an error when moderation fails", async () => {
    mocks.updateCourseReviewModeration.mockResolvedValue({
      success: false,
      error: "Forbidden",
    });
    const user = userEvent.setup();
    render(
      <TestimonialsManager
        courseId="course-1"
        testimonials={{
          total: 1,
          averageRating: 4,
          ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 1, 5: 0 },
          testimonials: [
            {
              id: "review-1",
              courseId: "course-1",
              studentId: "student-1",
              studentName: "Student One",
              rating: 4,
              title: "Useful",
              content: "Useful course.",
              featured: false,
              approved: false,
              verified: false,
              helpful: 0,
              createdAt: "2026-07-10T00:00:00.000Z",
              updatedAt: "2026-07-10T00:00:00.000Z",
            },
          ],
        }}
      />,
    );

    const approve = screen.getByRole("switch", {
      name: "Approve review Useful",
    });
    await user.click(approve);

    expect(await screen.findByRole("alert")).toHaveTextContent("Forbidden");
    expect(approve).not.toBeChecked();
  });

  it("renders the empty review state", () => {
    render(
      <TestimonialsManager
        courseId="course-1"
        testimonials={{
          total: 0,
          averageRating: 0,
          ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 },
          testimonials: [],
        }}
      />,
    );

    expect(
      screen.getByText("No course reviews have been submitted yet."),
    ).toBeInTheDocument();
  });

  it("unapproves a featured review and preserves other reviews", async () => {
    const user = userEvent.setup();
    render(
      <TestimonialsManager
        courseId="course-1"
        testimonials={{
          total: 2,
          averageRating: 4.5,
          ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 1, 5: 1 },
          testimonials: [
            {
              id: "review-1",
              courseId: "course-1",
              studentId: "student-1",
              studentName: "Student One",
              rating: 5,
              title: "Featured review",
              content: "",
              featured: true,
              approved: true,
              verified: true,
              helpful: 2,
              createdAt: "2026-07-10T00:00:00.000Z",
              updatedAt: "2026-07-10T00:00:00.000Z",
            },
            {
              id: "review-2",
              courseId: "course-1",
              studentId: "student-2",
              studentName: "Student Two",
              rating: 4,
              title: "Keep this review",
              content: "Still visible.",
              featured: false,
              approved: true,
              verified: false,
              helpful: 1,
              createdAt: "2026-07-11T00:00:00.000Z",
              updatedAt: "2026-07-11T00:00:00.000Z",
            },
          ],
        }}
      />,
    );

    expect(screen.getByText("No written review.")).toBeInTheDocument();
    await user.click(
      screen.getByRole("switch", { name: "Approve review Featured review" }),
    );

    await waitFor(() =>
      expect(mocks.updateCourseReviewModeration).toHaveBeenCalledWith(
        "course-1",
        "review-1",
        false,
        false,
      ),
    );
    expect(screen.getByText("Keep this review")).toBeInTheDocument();
    expect(
      screen.getByRole("switch", { name: "Feature review Featured review" }),
    ).toHaveAttribute("aria-disabled", "true");
  });
});
