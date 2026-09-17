import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  LearnerActivityForm,
  type LearnerActivityDescriptor,
} from "./learner-activity-form";

const actions = vi.hoisted(() => ({
  submitAssessment: vi.fn(),
  submitContentActivity: vi.fn(),
}));

vi.mock("@/lib/learner/activity-actions", () => actions);

vi.mock("next/navigation", () => ({
  usePathname: () => "/workspace/learning",
  useRouter: () => ({ refresh: vi.fn() }),
}));

const formProps = {
  courseId: "course-1",
  courseSlug: "game-production",
  enrollmentId: "enrollment-1",
};

function contentActivity(
  overrides: Partial<
    Extract<LearnerActivityDescriptor, { kind: "content" }>
  > = {},
): Extract<LearnerActivityDescriptor, { kind: "content" }> {
  return {
    kind: "content",
    contentId: "discussion-1",
    contentType: "Discussion",
    title: "Production discussion",
    ...overrides,
  };
}

function assessmentActivity(
  overrides: Partial<
    Extract<LearnerActivityDescriptor, { kind: "assessment" }>
  > = {},
): Extract<LearnerActivityDescriptor, { kind: "assessment" }> {
  return {
    kind: "assessment",
    assessment: {
      id: "assessment-1",
      title: "Course assessment",
      type: "Essay",
    },
    ...overrides,
  };
}

function renderActivity(activity: LearnerActivityDescriptor) {
  return render(<LearnerActivityForm {...formProps} activity={activity} />);
}

describe("LearnerActivityForm", () => {
  beforeEach(() => {
    actions.submitAssessment.mockReset();
    actions.submitContentActivity.mockReset();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("uses a form action and preserves the content activity identifiers", () => {
    const { container } = renderActivity(contentActivity());

    expect(
      screen.getByRole("button", { name: "Submit discussion" }),
    ).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText(
        "Add a constructive contribution to the course conversation.",
      ),
    ).toBeInTheDocument();
    expect(container.querySelector("form")).toHaveAttribute("action");
    expect(container.querySelector('input[name="courseId"]')).toHaveValue(
      "course-1",
    );
    expect(container.querySelector('input[name="contentId"]')).toHaveValue(
      "discussion-1",
    );
    expect(container.querySelector('input[name="kind"]')).toHaveValue(
      "discussion",
    );
  });

  it.each([
    ["Reflection", "Your reflection", "Submit reflection"],
    ["Survey", "Your response", "Submit survey"],
  ] as const)(
    "renders and updates a %s response",
    async (contentType, label, submitLabel) => {
      const user = userEvent.setup();
      renderActivity(contentActivity({ contentType }));

      const response = screen.getByLabelText(label);
      await user.type(response, "A considered response");

      expect(response).toHaveValue("A considered response");
      expect(response).toHaveAttribute("rows", "8");
      expect(response).toHaveAttribute(
        "placeholder",
        "Write your response here.",
      );
      expect(screen.getByRole("button", { name: submitLabel })).toBeEnabled();
    },
  );

  it("shows an already completed content activity", () => {
    renderActivity(contentActivity({ completed: true }));

    expect(screen.getByText("Activity completed")).toBeInTheDocument();
    expect(
      screen.getByText("Your course response has already been submitted."),
    ).toBeInTheDocument();
  });

  it("renders project choices as a native control and ignores invalid projects", async () => {
    const user = userEvent.setup();
    const { container } = renderActivity(
      assessmentActivity({
        assessment: {
          id: "assessment-1",
          title: "Portfolio project",
          type: "Project",
        },
        projects: [
          {
            id: "project-1",
            title: "Learner portfolio game",
            slug: "learner-portfolio-game",
            status: "Published",
            visibility: "Public",
            createdAt: "2026-08-01T00:00:00Z",
            updatedAt: "2026-08-01T00:00:00Z",
          },
          { id: "project-2" },
          { title: "Missing identifier" },
        ],
      }),
    );

    const select = container.querySelector('select[name="response"]');
    expect(select).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "Learner portfolio game" }),
    ).toHaveValue("project-1");
    expect(
      screen.getByRole("option", { name: "Untitled project" }),
    ).toHaveValue("project-2");
    expect(screen.queryByText("Missing identifier")).not.toBeInTheDocument();

    await user.selectOptions(select!, "project-2");
    expect(select).toHaveValue("project-2");
    expect(
      screen.getByRole("button", { name: "Submit assessment" }),
    ).toBeEnabled();
  });

  it("keeps quiz attempts out of the generic assessment form", () => {
    render(
      <LearnerActivityForm
        courseId="course-1"
        courseSlug="game-production"
        enrollmentId="enrollment-1"
        activity={{
          kind: "assessment",
          assessment: {
            id: "quiz-1",
            title: "Knowledge check",
            type: "Quiz",
            submissionModalities: "StructuredAnswer",
          },
        }}
      />,
    );

    expect(screen.getByText("Quiz attempt unavailable")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Submit assessment" }),
    ).not.toBeInTheDocument();
  });

  it("directs project assessments without projects to the configured projects URL", () => {
    vi.stubEnv("NEXT_PUBLIC_WEB_URL", "https://gameguild.example");
    renderActivity(
      assessmentActivity({
        assessment: {
          id: "assessment-1",
          title: "Portfolio project",
          type: "Project",
        },
      }),
    );

    expect(
      screen.getByText("Create a project before submitting"),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open projects" })).toHaveAttribute(
      "href",
      "https://gameguild.example/projects",
    );
    expect(
      screen.getByRole("button", { name: "Submit assessment" }),
    ).toBeDisabled();
  });

  it("uses the local projects URL when no public web URL is configured", () => {
    vi.stubEnv("NEXT_PUBLIC_WEB_URL", "");
    renderActivity(
      assessmentActivity({
        assessment: {
          id: "assessment-1",
          title: "Portfolio project",
          type: "Project",
        },
        projects: [],
      }),
    );

    expect(screen.getByRole("link", { name: "Open projects" })).toHaveAttribute(
      "href",
      "http://localhost:3000/projects",
    );
  });

  it.each([
    ["Url", "https://example.com/submission"],
    ["Media", "https://example.com/video"],
  ] as const)("accepts a %s assessment submission", async (modality, value) => {
    const user = userEvent.setup();
    renderActivity(
      assessmentActivity({
        assessment: {
          id: "assessment-1",
          title: `${modality} assessment`,
          type: "Essay",
          submissionModalities: modality,
        },
      }),
    );

    const response = screen.getByLabelText("Submission URL");
    await user.type(response, value);

    expect(response).toHaveAttribute("type", "url");
    expect(response).toHaveValue(value);
  });

  it("renders a code assessment with an expanded monospace editor", async () => {
    const user = userEvent.setup();
    renderActivity(
      assessmentActivity({
        assessment: {
          id: "assessment-1",
          title: "Code assessment",
          type: "Essay",
          submissionModalities: "Code",
        },
      }),
    );

    const response = screen.getByLabelText("Your code");
    await user.type(response, "return true;");

    expect(response).toHaveValue("return true;");
    expect(response).toHaveAttribute("rows", "14");
    expect(response).toHaveClass("font-mono");
  });

  it("renders the default assessment label", () => {
    renderActivity(
      assessmentActivity({
        assessment: {
          id: "essay-1",
          title: "Essay",
          type: "Essay",
          submissionModalities: "None",
        },
      }),
    );
    expect(screen.getByLabelText("Your submission")).toBeInTheDocument();
  });

  it("renders a private file assessment upload", () => {
    renderActivity(
      assessmentActivity({
        assessment: {
          id: "assessment-1",
          title: "File assessment",
          type: "Essay",
          submissionModalities: "File",
        },
      }),
    );

    expect(screen.getByLabelText("Choose file")).toHaveAttribute(
      "type",
      "file",
    );
    expect(
      screen.getByText(
        "The file is stored privately and linked to this assessment attempt.",
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Submit assessment" }),
    ).toBeEnabled();
  });

  it("shows a scored final submission with instructor feedback", () => {
    renderActivity(
      assessmentActivity({
        submission: {
          status: "Graded",
          score: 9200,
          feedback: "Strong solution.",
        },
      }),
    );

    expect(screen.getByText("Graded")).toBeInTheDocument();
    expect(screen.getByText("92 points")).toBeInTheDocument();
    expect(screen.getByText("Instructor feedback")).toBeInTheDocument();
    expect(screen.getByText("Strong solution.")).toBeInTheDocument();
  });

  it("shows an ungraded final submission without an empty feedback region", () => {
    renderActivity(
      assessmentActivity({
        submission: {
          status: "Submitted",
          score: null,
          feedback: null,
        },
      }),
    );

    expect(screen.getByText("Awaiting grading")).toBeInTheDocument();
    expect(screen.queryByText("Instructor feedback")).not.toBeInTheDocument();
  });

  it("keeps an in-progress assessment editable", () => {
    renderActivity(
      assessmentActivity({
        submission: { status: "InProgress" },
      }),
    );

    expect(screen.getByLabelText("Your submission")).toBeInTheDocument();
  });

  it("shows the success state returned by a content action", async () => {
    actions.submitContentActivity.mockResolvedValue({ success: true });
    const user = userEvent.setup();
    renderActivity(contentActivity());

    await user.type(screen.getByLabelText("Your contribution"), "My response");
    await user.click(screen.getByRole("button", { name: "Submit discussion" }));

    expect(await screen.findByText("Submission received")).toBeInTheDocument();
    expect(actions.submitContentActivity).toHaveBeenCalledOnce();
  });

  it("shows submission errors returned by an assessment action", async () => {
    actions.submitAssessment.mockResolvedValue({
      success: false,
      error: "Submission could not be stored.",
    });
    const user = userEvent.setup();
    renderActivity(assessmentActivity());

    await user.type(screen.getByLabelText("Your submission"), "My answer");
    await user.click(screen.getByRole("button", { name: "Submit assessment" }));

    expect(await screen.findByText("Submission failed")).toBeInTheDocument();
    expect(
      screen.getByText("Submission could not be stored."),
    ).toBeInTheDocument();
    expect(actions.submitAssessment).toHaveBeenCalledOnce();
  });

  it("disables the form while an assessment submission is pending", async () => {
    let resolveSubmission!: (value: { success: boolean }) => void;
    actions.submitAssessment.mockReturnValue(
      new Promise((resolve) => {
        resolveSubmission = resolve;
      }),
    );
    renderActivity(assessmentActivity());

    fireEvent.change(screen.getByLabelText("Your submission"), {
      target: { value: "Pending answer" },
    });
    fireEvent.submit(
      screen
        .getByRole("button", { name: "Submit assessment" })
        .closest("form")!,
    );

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Submitting..." }),
      ).toBeDisabled();
    });

    await act(async () => {
      resolveSubmission({ success: true });
    });
    expect(await screen.findByText("Submission received")).toBeInTheDocument();
  });
});
