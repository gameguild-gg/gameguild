import "@testing-library/jest-dom/vitest";

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ActivityComponent } from "./activity-component";

const mocks = vi.hoisted(() => ({
  submitActivity: vi.fn(),
}));

vi.mock("@/lib/courses/server-actions", () => ({
  submitActivity: mocks.submitActivity,
}));

vi.mock("@game-guild/quiz-surface/player", () => ({
  QuizPlayer: ({ entry }: { entry: { stem: string } }) => (
    <div data-testid="server-player">{entry.stem}</div>
  ),
  QuizPracticePlayer: ({
    entry,
    onAnswerChange,
  }: {
    entry: { stem: string };
    onAnswerChange: (answer: { type: "TRUE_FALSE"; value: boolean }) => void;
  }) => (
    <div>
      <span data-testid="practice-player">{entry.stem}</span>
      <button
        type="button"
        onClick={() => onAnswerChange({ type: "TRUE_FALSE", value: true })}
      >
        Choose true
      </button>
    </div>
  ),
}));

const quizItem = {
  id: "3d85ccca-7428-4fc9-88c7-13670a98d0f1",
  title: "Canonical quiz",
  type: "quiz" as const,
  status: "available" as const,
  order: 1,
  isRequired: true,
  activityType: "quiz" as const,
  content: {
    order: [["question-1", "quiz"]],
    blocks: {
      "question-1": {
        type: "TRUE_FALSE",
        stem: "The package owns this question",
        correctAnswer: true,
        points: 2,
        settings: {
          allowRetry: true,
          showFeedback: true,
          showCorrectAnswer: true,
        },
      },
    },
  },
};

describe("ActivityComponent quiz integration", () => {
  beforeEach(() => {
    mocks.submitActivity.mockReset();
    mocks.submitActivity.mockResolvedValue({ success: true });
  });

  it("renders canonical quiz storage with the package player and submits typed answers", async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();

    render(
      <ActivityComponent
        item={quizItem}
        courseId="course-1"
        onComplete={onComplete}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Start Activity" }));
    expect(screen.getByTestId("practice-player")).toHaveTextContent(
      "The package owns this question",
    );

    await user.click(screen.getByRole("button", { name: "Choose true" }));
    await user.click(screen.getByRole("button", { name: "Submit Quiz" }));

    expect(mocks.submitActivity).toHaveBeenCalledWith(
      expect.objectContaining({
        content: {
          answers: {
            "question-1": { selectedOptionIds: ["true"] },
          },
        },
        isGraded: false,
      }),
    );
    expect(onComplete).toHaveBeenCalledWith(100);
  });
});
