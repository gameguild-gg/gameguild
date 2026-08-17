import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AssessmentEditor } from "./assessment-editor";
import { deleteAssessment, updateAssessment } from "@/lib/learning/actions";
import type {
  Assessment,
  AssessmentGroup,
} from "@/lib/learning/queries/assessments";
import type { CourseContentItemViewModel } from "@/lib/learning/queries/course";

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
  usePathname: () => '/workspace/learning',
  useRouter: () => routerMocks,
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    href,
    children,
    ...props
  }: {
    href: string;
    children: React.ReactNode;
  }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock("@/lib/learning/actions", () => ({
  updateAssessment: vi.fn(),
  deleteAssessment: vi.fn(),
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
  gradingMethods: "InstructorGraded",
  groupSetId: null,
  peerReviewsRequiredCount: 0,
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

const courseContent = [
  {
    id: "content-assignment-1",
    parentId: null,
    order: 0,
    type: "Assignment",
    title: "Module 1 Homework",
    description: null,
    status: "published",
    duration: null,
    metadata: {},
    gradingConfig: null,
    createdAt: "2026-07-01T00:00:00.000Z",
    updatedAt: "2026-07-01T00:00:00.000Z",
  },
  {
    id: "content-lesson-1",
    parentId: null,
    order: 1,
    type: "Lesson",
    title: "Intro Lesson",
    description: null,
    status: "published",
    duration: null,
    metadata: {},
    gradingConfig: null,
    createdAt: "2026-07-01T00:00:00.000Z",
    updatedAt: "2026-07-01T00:00:00.000Z",
  },
] satisfies CourseContentItemViewModel[];

describe("AssessmentEditor", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateAssessment).mockResolvedValue({
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
        presentationMode: "Continuous",
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
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
      "/workspace/learning/courses/course-1/assessments",
    );
    expect(routerMocks.back).not.toHaveBeenCalled();
  });

  it("renders linked content items from courseContent", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        courseContent={courseContent}
      />,
    );

    await user.click(screen.getByRole("combobox", { name: /linked content/i }));
    expect(await screen.findByText("Module 1 Homework")).toBeInTheDocument();
    expect(screen.getByText("Intro Lesson")).toBeInTheDocument();
  });

  it("calls updateAssessment with contentId when an item is selected", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        courseContent={courseContent}
      />,
    );

    await user.click(screen.getByRole("combobox", { name: /linked content/i }));
    await user.click(screen.getByRole("option", { name: "Module 1 Homework" }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        contentId: "content-assignment-1",
        clearContentId: false,
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
  });

  it("unlinks content when None is selected (clearContentId: true)", async () => {
    const user = userEvent.setup();
    const linked = { ...assessment, contentId: "content-assignment-1" };
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={linked}
        assessmentGroups={groups}
        courseContent={courseContent}
      />,
    );

    await user.click(screen.getByRole("combobox", { name: /linked content/i }));
    await user.click(
      screen.getByRole("option", { name: /none \(standalone assessment\)/i }),
    );

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        contentId: null,
        clearContentId: true,
      });
    });
  });

  it("toggles grading methods and writes the comma-separated string", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        courseContent={courseContent}
      />,
    );

    // Given: assessment starts with only InstructorGraded
    expect(screen.getByRole("checkbox", { name: /instructorgraded/i })).toBeChecked();
    expect(screen.getByRole("checkbox", { name: /peerreview/i })).not.toBeChecked();

    await user.click(screen.getByRole("checkbox", { name: /peerreview/i }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        gradingMethods: "InstructorGraded,PeerReview",
      });
    });
  });

  it("removes a flag from the grading methods string when unchecked", async () => {
    const user = userEvent.setup();
    const multi = { ...assessment, gradingMethods: "PeerReview,AutoGraded" };
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={multi}
        assessmentGroups={groups}
        courseContent={courseContent}
      />,
    );

    await user.click(screen.getByRole("checkbox", { name: /autograded/i }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        gradingMethods: "PeerReview",
      });
    });
  });

  it("keeps the type selector disabled for existing assessments", () => {
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        courseContent={courseContent}
      />,
    );

    const typeTrigger = screen.getByText("Type cannot be changed after creation.")
      .closest("div")
      ?.querySelector('[role="combobox"]');
    expect(typeTrigger).toBeDisabled();
  });

  it("renders the grade submissions link in the header for instructors", () => {
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        courseContent={courseContent}
        canManage
      />,
    );

    const gradeButton = screen.getByTestId("grade-submissions-button");
    expect(gradeButton).toHaveAttribute(
      "href",
      "/dashboard/learning/courses/course-1/assessments/assessment-1/submissions",
    );
    expect(gradeButton).toHaveTextContent(/grade submissions/i);
  });

  it("hides the grade submissions link from non-instructors", () => {
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        courseContent={courseContent}
      />,
    );

    expect(
      screen.queryByTestId("grade-submissions-button"),
    ).not.toBeInTheDocument();
  });
});
