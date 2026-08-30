import "@testing-library/jest-dom/vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AssessmentEditor } from "./assessment-editor";
import {
  deleteAssessment,
  deleteRubric,
  saveRubric,
  updateAssessment,
} from "@/lib/learning/actions";
import type {
  Assessment,
  AssessmentGroup,
} from "@/lib/learning/queries/assessments";

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
  usePathname: () => "/workspace/learning/courses/course-1/assessments/assessment-1",
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
  saveRubric: vi.fn(),
  deleteRubric: vi.fn(),
}));

const assessment = {
  id: "assessment-1",
  courseId: "course-1",
  contentId: null,
  assessmentGroupId: null,
  assessmentGroupName: null,
  assessmentGroupWeightPercent: null,
  assessmentGroupOrder: null,
  title: "Group Project",
  description: "Build something together.",
  type: "Project",
  maxScore: 30,
  passingScore: 20,
  timeLimitMinutes: null,
  maxAttempts: null,
  isRequired: true,
  order: 1,
  availableFrom: null,
  availableUntil: null,
  presentationMode: "Continuous",
  dueAt: null,
  allowLateSubmissions: false,
  lateSubmissionDeadline: null,
  isAvailable: true,
  gradingMethods: "InstructorGraded",
  groupSetId: null,
  peerReviewsRequiredCount: 0,
} satisfies Assessment;

const groups = [] satisfies AssessmentGroup[];

const groupSets = [
  { id: "set-1", name: "Project teams" },
  { id: "set-2", name: "Lab pairs" },
];

describe("AssessmentEditor policy sections", () => {
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
    vi.mocked(saveRubric).mockResolvedValue({ success: true, data: null });
    vi.mocked(deleteRubric).mockResolvedValue({ success: true, data: null });
  });

  it("peer review toggle reveals required reviews defaulting to 3 (min 1)", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    expect(screen.queryByLabelText(/required reviews/i)).not.toBeInTheDocument();

    await user.click(screen.getByRole("switch", { name: /peer review/i }));

    const countInput = screen.getByLabelText(/required reviews/i);
    expect(countInput).toHaveValue(3);
    expect(countInput).toHaveAttribute("min", "1");

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        gradingMethods: "InstructorGraded,PeerReview",
        peerReviewsRequiredCount: 3,
      });
    });
  });

  it("turning peer review off removes the flag but keeps the count server-side", async () => {
    const user = userEvent.setup();
    const withPeer = {
      ...assessment,
      gradingMethods: "InstructorGraded,PeerReview",
      peerReviewsRequiredCount: 4,
    };
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={withPeer}
        assessmentGroups={groups}
      />,
    );

    expect(screen.getByLabelText(/required reviews/i)).toHaveValue(4);

    await user.click(screen.getByRole("switch", { name: /peer review/i }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        gradingMethods: "InstructorGraded",
      });
    });
    expect(
      vi.mocked(updateAssessment).mock.calls.at(-1)?.[0],
    ).not.toHaveProperty("peerReviewsRequiredCount");
  });

  it("group assignment toggle reveals the group set dropdown with course sets", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        groupSets={groupSets}
      />,
    );

    expect(screen.queryByRole("combobox", { name: /group set/i })).not.toBeInTheDocument();

    await user.click(
      screen.getByRole("switch", { name: /group assignment/i }),
    );

    await user.click(screen.getByRole("combobox", { name: /group set/i }));
    expect(await screen.findByRole("option", { name: "Project teams" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Lab pairs" })).toBeInTheDocument();

    await user.click(screen.getByRole("option", { name: "Project teams" }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        groupSetId: "set-1",
        clearGroupSetId: false,
      });
    });
  });

  it("turning the group assignment toggle off clears the group set", async () => {
    const user = userEvent.setup();
    const grouped = { ...assessment, groupSetId: "set-1" };
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={grouped}
        assessmentGroups={groups}
        groupSets={groupSets}
      />,
    );

    expect(screen.getByRole("combobox", { name: /group set/i })).toBeInTheDocument();

    await user.click(
      screen.getByRole("switch", { name: /group assignment/i }),
    );

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        groupSetId: null,
        clearGroupSetId: true,
      });
    });
  });

  it("rubric editor blocks save while the criteria sum mismatches max score and saves when equal", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    await user.click(screen.getByRole("switch", { name: /grade by rubric/i }));

    await user.type(screen.getByLabelText(/criterion 1 description/i), "Design");
    await user.type(screen.getByLabelText(/criterion 1 points/i), "10");
    await user.click(screen.getByRole("button", { name: /add criterion/i }));
    await user.type(screen.getByLabelText(/criterion 2 description/i), "Correctness");
    await user.type(screen.getByLabelText(/criterion 2 points/i), "15");

    const indicator = screen.getByTestId("rubric-sum");
    expect(indicator).toHaveTextContent(/25\s*\/\s*30/);
    expect(indicator).toHaveClass(/red|destructive/);
    expect(screen.getByRole("button", { name: /save rubric/i })).toBeDisabled();
    expect(saveRubric).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText(/criterion 2 points/i));
    await user.type(screen.getByLabelText(/criterion 2 points/i), "20");

    expect(screen.getByTestId("rubric-sum")).toHaveTextContent(/30\s*\/\s*30/);
    expect(screen.getByTestId("rubric-sum")).toHaveClass(/green/);
    expect(screen.getByRole("button", { name: /save rubric/i })).toBeEnabled();

    await user.click(screen.getByRole("button", { name: /save rubric/i }));

    await waitFor(() => {
      expect(saveRubric).toHaveBeenCalledWith({
        assessmentId: "assessment-1",
        title: "Rubric",
        criteria: [
          { description: "Design", points: 10, order: 0 },
          { description: "Correctness", points: 20, order: 1 },
        ],
      });
    });
  });

  it("renders a disabled rubric editor with the lock message when locked", async () => {
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        rubric={{
          title: "Rubric",
          criteria: [{ description: "Design", points: 30, order: 0 }],
        }}
        rubricLocked
      />,
    );

    expect(
      screen.getByText("Rubric locked after grading started"),
    ).toBeInTheDocument();
    expect(screen.getByRole("switch", { name: /grade by rubric/i })).toHaveAttribute("aria-disabled", "true");
    expect(screen.getByRole("button", { name: /save rubric/i })).toBeDisabled();
    expect(screen.getByLabelText(/criterion 1 description/i)).toBeDisabled();
  });

  it("locks the rubric editor after a 409 save failure", async () => {
    const user = userEvent.setup();
    vi.mocked(saveRubric).mockResolvedValueOnce({
      success: false,
      error: "Rubric locked after grading started",
    });
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    await user.click(screen.getByRole("switch", { name: /grade by rubric/i }));
    await user.type(screen.getByLabelText(/criterion 1 description/i), "Design");
    await user.type(screen.getByLabelText(/criterion 1 points/i), "30");
    await user.click(screen.getByRole("button", { name: /save rubric/i }));

    expect(
      await screen.findByText("Rubric locked after grading started"),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /save rubric/i })).toBeDisabled();
  });

  it("deletes the rubric after confirmation", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("confirm", vi.fn(() => true));
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        rubric={{
          title: "Rubric",
          criteria: [{ description: "Design", points: 30, order: 0 }],
        }}
      />,
    );

    await user.click(screen.getByRole("button", { name: /delete rubric/i }));

    await waitFor(() => {
      expect(deleteRubric).toHaveBeenCalledWith("assessment-1");
    });
  });
});
