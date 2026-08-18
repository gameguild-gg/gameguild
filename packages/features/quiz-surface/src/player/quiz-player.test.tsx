import "@testing-library/jest-dom/vitest";

import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  QuizEntryType,
  createEmptyQuizAnswer,
  createSingleChoiceEntry,
  createTrueFalseEntry,
  toQuizLearnerEntry,
} from "@game-guild/quiz";
import { QuizPlayer } from "./quiz-player";
import { QuizPracticePlayer } from "./quiz-practice-player";

afterEach(cleanup);

describe("quiz players", () => {
  it("emits a typed answer from local practice controls", () => {
    const entry = createSingleChoiceEntry("Capital?");
    const onAnswerChange = vi.fn();
    render(
      <QuizPracticePlayer
        entry={entry}
        answer={createEmptyQuizAnswer(entry.type)}
        onAnswerChange={onAnswerChange}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: entry.options[1]!.text }));
    expect(onAnswerChange).toHaveBeenCalledWith({
      type: QuizEntryType.SingleChoice,
      optionId: entry.options[1]!.id,
    });
  });

  it("submits learner answers without requiring an answer key", () => {
    const authored = createSingleChoiceEntry("Capital?");
    const entry = toQuizLearnerEntry(authored);
    if (entry.type !== QuizEntryType.SingleChoice) throw new Error("Unexpected learner entry type");
    const answer = {
      type: QuizEntryType.SingleChoice,
      optionId: entry.options[0]!.id,
    } as const;
    const onSubmit = vi.fn();
    render(
      <QuizPlayer
        entry={entry}
        answer={answer}
        onAnswerChange={() => undefined}
        onSubmit={onSubmit}
      />,
    );

    const submitButton = screen.getByRole("button", { name: "Submit answer" });
    expect(submitButton).toHaveClass(
      "bg-blue-600",
      "shadow-sm",
      "transition-colors",
      "hover:bg-blue-700",
      "hover:shadow-md",
    );

    fireEvent.click(submitButton);
    expect(onSubmit).toHaveBeenCalledWith(answer);
  });

  it("shows server grading submission as the blue status banner", () => {
    const authored = createSingleChoiceEntry("Capital?");
    const entry = toQuizLearnerEntry(authored);

    render(
      <QuizPlayer
        entry={entry}
        answer={createEmptyQuizAnswer(entry.type)}
        onAnswerChange={() => undefined}
        onSubmit={() => undefined}
        submissionResult={{
          status: "pending",
        }}
      />,
    );

    const status = screen.getByRole("status");
    expect(status).toHaveTextContent("Answer submitted.");
    expect(status).toHaveClass(
      "h-12",
      "border-l-4",
      "border-blue-500",
      "bg-blue-50",
      "text-blue-700",
      "justify-between",
    );
    expect(status).not.toHaveClass("shadow-sm");
    expect(status.querySelector("svg")).toBeNull();
    expect(
      screen.queryByRole("button", { name: "Submit answer" }),
    ).not.toBeInTheDocument();
  });

  it("keeps a submitted state visible when practice feedback is hidden", () => {
    const baseEntry = createTrueFalseEntry("The Earth is round.");
    const entry = {
      ...baseEntry,
      settings: { ...baseEntry.settings, showFeedback: false },
    };

    render(
      <QuizPracticePlayer
        entry={entry}
        answer={createEmptyQuizAnswer(entry.type)}
        onAnswerChange={() => undefined}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Check answer" }));

    const status = screen.getByRole("status");
    expect(status).toHaveTextContent("Answer submitted.");
    expect(status).toHaveClass("border-blue-500", "bg-blue-50");
  });
});
