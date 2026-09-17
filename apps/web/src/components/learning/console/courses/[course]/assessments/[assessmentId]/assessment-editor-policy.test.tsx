import "@testing-library/jest-dom/vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  AssessmentEditor,
  resolvePeerReviewsRequiredCount,
} from "./assessment-editor";
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
import { createReviewMethods } from "@game-guild/grading";

const routerMocks = vi.hoisted(() => ({
  back: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
  replace: vi.fn(),
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
  usePathname: () =>
    "/workspace/learning/courses/course-1/assessments/assessment-1",
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
  slug: "assessment-1",
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
  reviewMethods: createReviewMethods("InstructorReview"),
  groupSetId: null,
  publishedDefinitionRevisionId: null,
  reviewConfigurationCanonicalJson: null,
  attemptContributionMode: null,
  contentCompletionMode: "on-release-and-pass",
  resultReleaseMode: "manual",
  resultReleaseScheduledFor: null,
  version: 1,
} satisfies Assessment;

const groups = [] satisfies AssessmentGroup[];

const groupSets = [
  { id: "set-1", name: "Project teams" },
  { id: "set-2", name: "Lab pairs" },
];

describe("AssessmentEditor policy sections", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(updateAssessment).mockResolvedValue({
      success: true,
      data: { version: 2 },
    });
    vi.mocked(deleteAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(saveRubric).mockResolvedValue({ success: true, data: null });
    vi.mocked(deleteRubric).mockResolvedValue({ success: true, data: null });
    vi.stubGlobal(
      "confirm",
      vi.fn(() => true),
    );
  });

  it("selecting peer review reveals its required-review policy", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    expect(
      screen.queryByLabelText(/required peer reviews/i),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole("combobox", { name: /primary review/i }));
    await user.click(
      await screen.findByRole("option", { name: "Peer review" }),
    );

    const countInput = screen.getByLabelText(/required peer reviews/i);
    expect(countInput).toHaveValue(3);
    expect(countInput).toHaveAttribute("min", "1");

    await user.click(screen.getByRole("button", { name: /save changes/i }));
    await waitFor(() => expect(updateAssessment).toHaveBeenCalled());
    const call = vi.mocked(updateAssessment).mock.calls.at(-1)![0];
    expect(call.reviewMethods).toBe(1);
    expect(
      JSON.parse(call.reviewConfigurationCanonicalJson!).peer,
    ).toMatchObject({ reviewsRequiredPerSubmission: 3 });
  });

  it("switching away from peer review removes its owned configuration", async () => {
    const user = userEvent.setup();
    const withPeer = {
      ...assessment,
      reviewMethods: createReviewMethods("PeerReview", true),
      reviewConfigurationCanonicalJson: JSON.stringify({
        schemaVersion: 1,
        peer: {
          reviewsPerReviewer: 4,
          reviewsRequiredPerSubmission: 4,
          minimumReviewsToFinalize: 4,
          aggregation: "median",
          claimLeaseMinutes: 15,
          evidenceWindowMinutes: 1440,
          onInsufficientEvidence: "await-instructor",
        },
        instructor: { requireOverrideReason: false },
      }),
    };
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={withPeer}
        assessmentGroups={groups}
      />,
    );

    expect(screen.getByLabelText(/required peer reviews/i)).toHaveValue(4);

    await user.click(screen.getByRole("combobox", { name: /primary review/i }));
    await user.click(
      await screen.findByRole("option", { name: "Instructor review" }),
    );
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => expect(updateAssessment).toHaveBeenCalled());
    const call = vi.mocked(updateAssessment).mock.calls.at(-1)![0];
    expect(call.reviewMethods).toBe(8);
    expect(JSON.parse(call.reviewConfigurationCanonicalJson!).peer).toBeNull();
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

    expect(
      screen.queryByRole("combobox", { name: /group set/i }),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole("switch", { name: /group assignment/i }));

    await user.click(screen.getByRole("combobox", { name: /group set/i }));
    expect(
      await screen.findByRole("option", { name: "Project teams" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "Lab pairs" }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole("option", { name: "Project teams" }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        expectedVersion: 1,
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

    expect(
      screen.getByRole("combobox", { name: /group set/i }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole("switch", { name: /group assignment/i }));

    await waitFor(() => {
      expect(updateAssessment).toHaveBeenCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        expectedVersion: 1,
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

    await user.type(
      screen.getByLabelText(/criterion 1 description/i),
      "Design",
    );
    await user.type(screen.getByLabelText(/criterion 1 points/i), "10");
    await user.click(screen.getByRole("button", { name: /add criterion/i }));
    await user.type(
      screen.getByLabelText(/criterion 2 description/i),
      "Correctness",
    );
    await user.type(screen.getByLabelText(/criterion 2 points/i), "15");

    const indicator = screen.getByTestId("rubric-sum");
    expect(indicator).toHaveTextContent(/25\s*\/\s*30/);
    expect(indicator).toHaveClass(/red|destructive/);
    expect(screen.getByRole("button", { name: /save rubric/i })).toBeDisabled();
    expect(saveRubric).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText(/criterion 2 points/i));
    await user.type(screen.getByLabelText(/criterion 2 points/i), "21");
    expect(screen.getByTestId("rubric-sum")).toHaveTextContent(/\+1/);

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
    expect(
      screen.getByRole("switch", { name: /grade by rubric/i }),
    ).toHaveAttribute("aria-disabled", "true");
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
    await user.type(
      screen.getByLabelText(/criterion 1 description/i),
      "Design",
    );
    await user.type(screen.getByLabelText(/criterion 1 points/i), "30");
    await user.click(screen.getByRole("button", { name: /save rubric/i }));

    expect(
      await screen.findByText("Rubric locked after grading started"),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /save rubric/i })).toBeDisabled();
  });

  it("deletes the rubric after confirmation", async () => {
    const user = userEvent.setup();
    vi.stubGlobal(
      "confirm",
      vi.fn(() => true),
    );
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

  it("rolls back group assignment when clearing the persisted group set fails", async () => {
    const user = userEvent.setup();
    vi.mocked(updateAssessment).mockResolvedValueOnce({
      success: false,
      error: "Group assignment update failed",
    });
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={{ ...assessment, groupSetId: "set-1" }}
        assessmentGroups={groups}
        groupSets={groupSets}
      />,
    );

    await user.click(screen.getByRole("switch", { name: /group assignment/i }));

    expect(
      await screen.findByText("Group assignment update failed"),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("switch", { name: /group assignment/i }),
    ).toBeChecked();
    expect(
      screen.getByRole("combobox", { name: /group set/i }),
    ).toHaveTextContent("Project teams");
  });

  it("rolls back a group-set selection failure", async () => {
    const user = userEvent.setup();
    vi.mocked(updateAssessment).mockResolvedValueOnce({
      success: false,
      error: "Group set failed",
    });
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={{ ...assessment, groupSetId: "set-1" }}
        assessmentGroups={groups}
        groupSets={groupSets}
      />,
    );

    const groupSet = screen.getByRole("combobox", { name: /group set/i });
    await user.click(groupSet);
    await user.click(await screen.findByRole("option", { name: "Lab pairs" }));
    expect(await screen.findByText("Group set failed")).toBeInTheDocument();
    expect(groupSet).toHaveTextContent("Project teams");
  });

  it("explicitly clears a selected group set", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={{ ...assessment, groupSetId: "set-1" }}
        assessmentGroups={groups}
        groupSets={groupSets}
      />,
    );

    const groupSet = screen.getByRole("combobox", { name: /group set/i });
    await user.click(groupSet);
    await user.click(
      await screen.findByRole("option", { name: "No group set" }),
    );
    await waitFor(() => {
      expect(updateAssessment).toHaveBeenLastCalledWith({
        courseId: "course-1",
        assessmentId: "assessment-1",
        expectedVersion: 1,
        groupSetId: null,
        clearGroupSetId: true,
      });
    });
  });

  it("reports a peer-review workflow save failure and keeps the draft", async () => {
    const user = userEvent.setup();
    vi.mocked(updateAssessment).mockResolvedValueOnce({
      success: false,
      error: "Peer review update failed",
    });
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    const primaryReview = screen.getByRole("combobox", {
      name: /primary review/i,
    });
    await user.click(primaryReview);
    await user.click(
      await screen.findByRole("option", { name: "Peer review" }),
    );
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    expect(
      await screen.findByText("Peer review update failed"),
    ).toBeInTheDocument();
    expect(primaryReview).toHaveTextContent("Peer review");
  });

  it("persists and reports failures for the peer review count", async () => {
    const user = userEvent.setup();
    const withPeer = {
      ...assessment,
      reviewMethods: createReviewMethods("PeerReview", true),
      reviewConfigurationCanonicalJson: JSON.stringify({
        schemaVersion: 1,
        peer: {
          reviewsPerReviewer: 4,
          reviewsRequiredPerSubmission: 4,
          minimumReviewsToFinalize: 4,
          aggregation: "mean",
          claimLeaseMinutes: 30,
          evidenceWindowMinutes: 10080,
          onInsufficientEvidence: "await-instructor-resolution",
        },
        instructor: { requireOverrideReason: false },
      }),
    };
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={withPeer}
        assessmentGroups={groups}
      />,
    );
    const requiredReviews = screen.getByLabelText(/required peer reviews/i);
    expect(requiredReviews).toHaveValue(4);

    await user.clear(requiredReviews);
    await user.type(requiredReviews, "5");
    await user.click(screen.getByRole("button", { name: /save changes/i }));
    await waitFor(() => expect(updateAssessment).toHaveBeenCalled());
    const saved = vi.mocked(updateAssessment).mock.calls.at(-1)![0];
    expect(saved.reviewMethods).toBe(9);
    expect(
      JSON.parse(saved.reviewConfigurationCanonicalJson!).peer,
    ).toMatchObject({ reviewsRequiredPerSubmission: 5 });
    expect(routerMocks.refresh).toHaveBeenCalled();
    await waitFor(() => expect(requiredReviews).toBeEnabled());

    vi.mocked(updateAssessment).mockResolvedValueOnce({
      success: false,
      error: "Count update failed",
    });
    await user.clear(requiredReviews);
    await user.type(requiredReviews, "6");
    await user.click(screen.getByRole("button", { name: /save changes/i }));
    expect(await screen.findByText("Count update failed")).toBeInTheDocument();
  });

  it("removes both the only rubric criterion and one of multiple criteria", async () => {
    const user = userEvent.setup();
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
      />,
    );

    await user.click(screen.getByRole("switch", { name: /grade by rubric/i }));
    await user.click(
      screen.getByRole("button", { name: "Remove criterion 1" }),
    );
    expect(screen.getByLabelText(/criterion 1 description/i)).toHaveValue("");

    await user.click(screen.getByRole("button", { name: /add criterion/i }));
    expect(
      screen.getByLabelText(/criterion 2 description/i),
    ).toBeInTheDocument();
    await user.click(
      screen.getByRole("button", { name: "Remove criterion 1" }),
    );
    expect(
      screen.queryByLabelText(/criterion 2 description/i),
    ).not.toBeInTheDocument();
  });

  it("shows a non-locking rubric save failure", async () => {
    const user = userEvent.setup();
    vi.mocked(saveRubric).mockResolvedValueOnce({
      success: false,
      error: "Rubric save failed",
    });
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

    await user.click(screen.getByRole("button", { name: /save rubric/i }));

    expect(await screen.findByText("Rubric save failed")).toBeInTheDocument();
    expect(
      screen.queryByText("Rubric locked after grading started"),
    ).not.toBeInTheDocument();
  });

  it("respects rubric deletion cancellation", async () => {
    const user = userEvent.setup();
    vi.mocked(confirm).mockReturnValueOnce(false);
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

    expect(deleteRubric).not.toHaveBeenCalled();
  });

  it.each([
    ["Rubric locked after grading started", true],
    ["Rubric delete failed", false],
  ])("handles rubric deletion failure: %s", async (error, locksRubric) => {
    const user = userEvent.setup();
    vi.mocked(deleteRubric).mockResolvedValueOnce({ success: false, error });
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

    if (locksRubric) {
      expect(
        await screen.findByText("Rubric locked after grading started"),
      ).toBeInTheDocument();
      expect(
        screen.getByRole("button", { name: /save rubric/i }),
      ).toBeDisabled();
    } else {
      expect(await screen.findByText(error)).toBeInTheDocument();
    }
  });

  it("starts an empty stored rubric with one editable criterion", () => {
    render(
      <AssessmentEditor
        courseId="course-1"
        assessment={assessment}
        assessmentGroups={groups}
        rubric={{ title: "Empty rubric", criteria: [] }}
      />,
    );

    expect(screen.getByLabelText(/criterion 1 description/i)).toHaveValue("");
    expect(screen.getByLabelText(/criterion 1 points/i)).toHaveValue(null);
  });

  it("normalizes peer-review counts and defaults zero to three", () => {
    expect(resolvePeerReviewsRequiredCount("2")).toBe(2);
    expect(resolvePeerReviewsRequiredCount("0")).toBe(3);
  });
});
