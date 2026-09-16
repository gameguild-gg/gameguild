import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const routerMocks = vi.hoisted(() => ({ push: vi.fn() }));
const gradeActionMock = vi.hoisted(() => ({ gradeSubmission: vi.fn() }));
const buttonMock = vi.hoisted(() => ({
  confirm: null as (() => void | Promise<void>) | null,
}));
const graderMock = vi.hoisted(() => ({
  props: null as {
    assignment?: unknown;
    submittedFiles?: unknown;
    onComputedScore?: (result: { score: number; autoFeedback: string }) => void;
  } | null,
  result: { score: 67, autoFeedback: "Score: 67/100\nexpected c, got 0" },
}));

vi.mock("next/navigation", () => ({
  useRouter: () => routerMocks,
  usePathname: () => "/workspace/learning",
}));

vi.mock("@/components/learning/assessment-grading/assessment-grader", async () => {
  const React = await import("react");
  return {
    mergeWorkspaceWithSubmission: (
      _assignment: unknown,
      submittedFiles: Array<{ path: string; content: string }>,
    ) => submittedFiles,
    AssessmentGrader: (props: typeof graderMock.props) => {
      graderMock.props = props;
      return React.createElement(
        "button",
        {
          type: "button",
          "data-testid": "grade-button",
          onClick: () => props?.onComputedScore?.(graderMock.result),
        },
        "Run full tests",
      );
    },
  };
});

vi.mock("@/lib/learning/grade-action", () => ({
  gradeSubmission: gradeActionMock.gradeSubmission,
}));

vi.mock("@game-guild/ui/components/button", async () => {
  const React = await import("react");
  return {
    Button: ({
      children,
      onClick,
      ...props
    }: React.ButtonHTMLAttributes<HTMLButtonElement>) => {
      if (props["data-testid"] === "confirm-grade-button") {
        buttonMock.confirm = onClick
          ? () => onClick({} as React.MouseEvent<HTMLButtonElement>)
          : null;
      }
      return React.createElement("button", { ...props, onClick }, children);
    },
  };
});

import { GradeClient } from "./grade-client";
import { gradeSubmission } from "@/lib/learning/grade-action";
import type { CodingAssignmentContent } from "@/lib/coding-assignment/client";

const assignment: CodingAssignmentContent = {
  Type: "coding-assignment",
  Version: 1,
  Environment: {
    Language: "cpp",
    Tools: "clang",
    AllowStudentCreateFiles: true,
  },
  Data: {
    Files: {
      "/home/user/main.cpp": {
        Content: "// starter",
        Encoding: "text",
        Visibility: "Public",
        Modifiable: true,
      },
      "/home/user/secret.cpp": {
        Content: "// instructor only",
        Encoding: "text",
        Visibility: "Private",
        Modifiable: false,
      },
    },
  },
  Tests: { Public: [], Private: [] },
  Grading: { MaxScore: 100 },
};

const baseProps = {
  courseSlug: "course-1",
  assessmentId: "assessment-1",
  assessmentSlug: "assessment-1",
  submissionId: "submission-1",
  assignment,
  submittedFiles: [
    { path: "/home/user/main.cpp", content: "// student main" },
    { path: "/home/user/secret.cpp", content: "// malicious override" },
  ],
  maxScore: 100,
  manifestUrl: "/cdn/manifest.json",
};

describe("GradeClient", () => {
  beforeEach(() => {
    graderMock.props = null;
    graderMock.result = {
      score: 67,
      autoFeedback: "Score: 67/100\nexpected c, got 0",
    };
    routerMocks.push.mockReset();
    vi.mocked(gradeSubmission).mockReset();
    buttonMock.confirm = null;
  });

  it("delegates full execution to the shared assessment grader", () => {
    render(<GradeClient {...baseProps} />);

    expect(graderMock.props?.assignment).toBe(assignment);
    expect(graderMock.props?.submittedFiles).toEqual(baseProps.submittedFiles);
    expect(screen.getByTestId("confirm-grade-button")).toBeDisabled();
  });

  it("ignores direct confirmation before a score is ready", async () => {
    render(<GradeClient {...baseProps} />);

    await buttonMock.confirm?.();

    expect(gradeSubmission).not.toHaveBeenCalled();
    expect(screen.queryByTestId("grade-result")).not.toBeInTheDocument();
  });

  it("uses the assessment result to enable confirmation without exposing private files for comments", async () => {
    render(<GradeClient {...baseProps} />);

    fireEvent.click(screen.getByTestId("grade-button"));
    await waitFor(() =>
      expect(screen.getByTestId("grade-score")).toHaveTextContent("67"),
    );

    expect(
      screen.getByTestId("comment-/home/user/main.cpp"),
    ).toBeInTheDocument();
    expect(
      screen.queryByTestId("comment-/home/user/secret.cpp"),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("confirm-grade-button")).not.toBeDisabled();
  });

  it("composes feedback and posts the score selected by the shared grader", async () => {
    vi.mocked(gradeSubmission).mockResolvedValue({
      success: true,
      data: { submissionId: "submission-1" },
    });
    render(<GradeClient {...baseProps} />);

    fireEvent.click(screen.getByTestId("grade-button"));
    await screen.findByTestId("grade-score");
    fireEvent.change(screen.getByTestId("overall-comment"), {
      target: { value: "Strong submission." },
    });
    fireEvent.change(screen.getByTestId("comment-/home/user/main.cpp"), {
      target: { value: "Rename loop counter." },
    });
    fireEvent.click(screen.getByTestId("confirm-grade-button"));

    await waitFor(() => expect(gradeSubmission).toHaveBeenCalledTimes(1));
    const call = vi.mocked(gradeSubmission).mock.calls[0][0];
    expect(call.score).toBe(67);
    expect(call.feedback).toContain("Strong submission.");
    expect(call.feedback).toContain("Rename loop counter.");
    expect(call.feedback).toContain("expected c, got 0");
    expect(routerMocks.push).toHaveBeenCalled();
  });

  it("keeps the assessed grade reviewable when the posting action fails", async () => {
    vi.mocked(gradeSubmission).mockResolvedValue({
      success: false,
      error: "Forbidden",
    });
    render(<GradeClient {...baseProps} />);

    fireEvent.click(screen.getByTestId("grade-button"));
    await screen.findByTestId("grade-score");
    fireEvent.click(screen.getByTestId("confirm-grade-button"));

    await waitFor(() =>
      expect(screen.getByTestId("grade-error")).toHaveTextContent("Forbidden"),
    );
    expect(screen.getByTestId("confirm-grade-button")).not.toBeDisabled();
  });

  it("recovers from typed and unknown exceptions while posting a grade", async () => {
    vi.mocked(gradeSubmission)
      .mockRejectedValueOnce(new Error("Grade service unavailable."))
      .mockRejectedValueOnce("connection lost");
    render(<GradeClient {...baseProps} />);

    fireEvent.click(screen.getByTestId("grade-button"));
    await screen.findByTestId("grade-score");
    fireEvent.click(screen.getByTestId("confirm-grade-button"));

    await waitFor(() =>
      expect(screen.getByTestId("grade-error")).toHaveTextContent(
        "Grade service unavailable.",
      ),
    );
    expect(screen.getByTestId("confirm-grade-button")).toBeEnabled();

    fireEvent.click(screen.getByTestId("confirm-grade-button"));
    await waitFor(() =>
      expect(screen.getByTestId("grade-error")).toHaveTextContent(
        "connection lost",
      ),
    );
    expect(screen.getByTestId("confirm-grade-button")).toBeEnabled();
    expect(routerMocks.push).not.toHaveBeenCalled();
  });
});
