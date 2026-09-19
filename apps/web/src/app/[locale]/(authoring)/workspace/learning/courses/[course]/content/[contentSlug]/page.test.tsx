import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getCourse: vi.fn(),
  getContentItem: vi.fn(),
  getCourseContent: vi.fn(),
  getCourseAssessments: vi.fn(),
  getCodingAssignmentFull: vi.fn(),
  getDraft: vi.fn(),
}));

vi.mock("@/lib/learning", () => ({
  getCourse: mocks.getCourse,
  getContentItem: mocks.getContentItem,
  getCourseContent: mocks.getCourseContent,
  getCourseAssessments: mocks.getCourseAssessments,
}));
vi.mock("@/lib/learning/authoring", () => ({
  getAuthoringDraft: mocks.getDraft,
}));
vi.mock("@/lib/coding-assignment/client", () => ({
  getCodingAssignmentFull: mocks.getCodingAssignmentFull,
}));
vi.mock("next/navigation", () => ({
  notFound: () => {
    throw new Error("not found");
  },
}));
vi.mock("@/components/learning/authoring/lesson-authoring-workspace", () => ({
  LessonAuthoringWorkspace: ({
    linkedAssessment,
    initialCodingAssignment,
  }: {
    linkedAssessment?: { id: string; slug: string } | null;
    initialCodingAssignment?: { Type: string } | null;
  }) => (
    <div data-testid="workspace">
      {linkedAssessment ? `${linkedAssessment.id}:${linkedAssessment.slug}` : "no-assessment"}
      {initialCodingAssignment ? `:${initialCodingAssignment.Type}` : ":no-coding-assignment"}
    </div>
  ),
}));

import ContentItemAuthoringPage from "./page";

describe("ContentItemAuthoringPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCourse.mockResolvedValue({ id: "course-1", slug: "course", title: "Course" });
    mocks.getCourseContent.mockResolvedValue({ items: [] });
    mocks.getDraft.mockResolvedValue({ success: true, data: { id: "draft-1" } });
    mocks.getCodingAssignmentFull.mockResolvedValue({
      Type: "coding-assignment",
    });
    mocks.getCourseAssessments.mockResolvedValue({
      assessments: [
        { id: "assessment-1", slug: "test-assessment", contentId: "content-1" },
      ],
      total: 1,
    });
  });

  it.each(["Code", "Questionnaire"])(
    "passes the linked assessment to %s authoring",
    async (type) => {
      mocks.getContentItem.mockResolvedValue({
        id: "content-1",
        slug: "test",
        type,
      });

      render(
        await ContentItemAuthoringPage({
          params: Promise.resolve({
            locale: "en-US",
            course: "course",
            contentSlug: "test",
          }),
        } as never),
      );

      expect(screen.getByTestId("workspace")).toHaveTextContent(
        type === "Code"
          ? "assessment-1:test-assessment:coding-assignment"
          : "assessment-1:test-assessment:no-coding-assignment",
      );
    },
  );

  it("loads the full coding assignment only for Code content", async () => {
    mocks.getContentItem.mockResolvedValue({
      id: "content-1",
      slug: "test",
      type: "Code",
    });

    render(
      await ContentItemAuthoringPage({
        params: Promise.resolve({
          locale: "en-US",
          course: "course",
          contentSlug: "test",
        }),
      } as never),
    );

    expect(mocks.getCodingAssignmentFull).toHaveBeenCalledWith(
      "course-1",
      "content-1",
    );
  });
});
