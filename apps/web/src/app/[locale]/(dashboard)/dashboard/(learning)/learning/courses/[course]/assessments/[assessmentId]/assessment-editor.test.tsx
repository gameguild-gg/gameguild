import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AssessmentEditor } from "./assessment-editor";
import {
  deleteAssessment,
  updateAssessment,
  updateAssessmentDefinition,
} from "@/lib/learning/actions";
import type {
  Assessment,
  AssessmentGroup,
} from "@/lib/learning/queries/assessments";
import type { ContentItem } from "@/lib/learning/types";

const routerMocks = vi.hoisted(() => ({
  back: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
}));

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

vi.mock("next/navigation", () => ({
  useRouter: () => routerMocks,
}));

vi.mock("@/lib/learning/actions", () => ({
  updateAssessment: vi.fn(),
  updateAssessmentDefinition: vi.fn(),
  deleteAssessment: vi.fn(),
}));

vi.mock("./quiz-assessment-editor", () => ({
  QuizAssessmentEditor: ({
    onChange,
  }: {
    onChange: (definition: unknown) => void;
  }) => (
    <button
      type="button"
      onClick={() =>
        onChange({
          order: [["1", "quiz"]],
          blocks: { "1": { type: "SINGLE_CHOICE" } },
        })
      }
    >
      Mock quiz editor
    </button>
  ),
}));

const assessment = {
  id: "assessment-1",
  courseId: "course-1",
  contentId: null,
  assessmentGroupId: "group-quizzes",
  assessmentGroupName: "Weekly quizzes",
  assessmentGroupWeightPercent: 30,
  assessmentGroupOrder: 1,
  title: "Schema Patterns Quiz",
  description: "Check understanding of schema patterns.",
  type: "Quiz",
  maxScore: 10,
  passingScore: 7,
  timeLimitMinutes: 30,
  maxAttempts: 2,
  isRequired: true,
  order: 1,
  availableFrom: "2026-07-01T10:00:00.000Z",
  availableUntil: "2026-07-05T10:00:00.000Z",
  presentationMode: "Continuous",
  dueAt: null,
  allowLateSubmissions: false,
  lateSubmissionDeadline: null,
  isAvailable: true,
} satisfies Assessment;

const groups = [
  {
    id: "group-quizzes",
    courseId: "course-1",
    name: "Weekly quizzes",
    description: null,
    weightPercent: 30,
    order: 1,
  },
] satisfies AssessmentGroup[];

const contentItems = [
  {
    id: "module-1",
    courseId: "course-1",
    parentId: null,
    title: "Foundations",
    description: null,
    type: "Module",
    order: 0,
    status: "published",
    duration: null,
    metadata: {},
    createdAt: "2026-07-01T00:00:00.000Z",
    updatedAt: "2026-07-01T00:00:00.000Z",
  },
  {
    id: "lesson-1",
    courseId: "course-1",
    parentId: "module-1",
    title: "Course overview",
    description: null,
    type: "Lesson",
    order: 0,
    status: "published",
    duration: 20,
    metadata: {},
    createdAt: "2026-07-01T00:00:00.000Z",
    updatedAt: "2026-07-01T00:00:00.000Z",
  },
] satisfies ContentItem[];

describe("AssessmentEditor", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(updateAssessmentDefinition).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(deleteAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.stubGlobal(
      "confirm",
      vi.fn(() => true),
    );
  });

  it("validates the title before updating assessment settings", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    await user.clear(screen.getByLabelText(/^title$/i));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    expect(await screen.findByText("Title is required.")).toBeInTheDocument();
    expect(updateAssessment).not.toHaveBeenCalled();
  });

  it("saves details, scoring, availability, and weighted group assignment", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    await user.clear(screen.getByLabelText(/^title$/i));
    await user.type(screen.getByLabelText(/^title$/i), "Updated Quiz");
    fireEvent.change(screen.getByLabelText(/description/i), {
      target: { value: "Updated instructions." },
    });
    fireEvent.change(screen.getByLabelText(/max score/i), {
      target: { value: "20" },
    });
    fireEvent.change(screen.getByLabelText(/passing score/i), {
      target: { value: "14" },
    });
    fireEvent.change(screen.getByLabelText(/available from/i), {
      target: { value: "2026-08-01T10:00" },
    });
    fireEvent.change(screen.getByLabelText(/available until/i), {
      target: { value: "2026-08-05T10:00" },
    });
    fireEvent.change(screen.getByLabelText(/time limit/i), {
      target: { value: "45" },
    });
    fireEvent.change(screen.getByLabelText(/max attempts/i), {
      target: { value: "3" },
    });
    await user.click(screen.getByRole("button", { name: /mock quiz editor/i }));

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        title: "Updated Quiz",
        description: "Updated instructions.",
        maxScore: 20,
        passingScore: 14,
        timeLimitMinutes: 45,
        maxAttempts: 3,
        isRequired: true,
        availableFrom: "2026-08-01T10:00",
        availableUntil: "2026-08-05T10:00",
        assessmentGroupId: "group-quizzes",
        clearAssessmentGroupId: false,
        contentId: null,
        clearContentId: true,
        presentationMode: "Continuous",
      });
      expect(updateAssessmentDefinition).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        definition: {
          order: [["1", "quiz"]],
          blocks: { "1": { type: "SINGLE_CHOICE" } },
        },
        definitionSchemaVersion: 1,
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
  });

  it("links the assessment to an instructional lesson", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        contentItems={contentItems}
      />,
    );

    await user.click(screen.getByLabelText(/linked lesson/i));
    await user.click(screen.getByRole("option", { name: /course overview/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith(
        expect.objectContaining({
          contentId: "lesson-1",
          clearContentId: false,
        }),
      );
    });
  });

  it("shows API errors and deletes after explicit confirmation", async () => {
    const user = userEvent.setup();
    vi.mocked(updateAssessment).mockResolvedValueOnce({
      success: false,
      error: "Bad Request",
    });

    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));
    expect(await screen.findByText("Bad Request")).toBeInTheDocument();

    await user.click(
      screen.getByRole("button", { name: /delete assessment/i }),
    );

    await waitFor(() => {
      expect(deleteAssessment).toHaveBeenCalledWith("course-1", "assessment-1");
    });
    expect(routerMocks.push).toHaveBeenCalledWith(
      "/dashboard/learning/courses/course-1/assessments",
    );
    expect(routerMocks.back).not.toHaveBeenCalled();
  });
});
