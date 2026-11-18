import "@testing-library/jest-dom/vitest";

import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { createEssayEntry } from "@game-guild/quiz";
import type { RendererAnswerState } from "../../player/renderer-answer-adapter";
import { EssayRenderer } from "./player";

vi.mock("./lexical-adapter", () => ({
  EssayLexicalEditor: ({ initialState }: { initialState?: unknown }) => (
    <output data-testid="essay-answer-state">
      {JSON.stringify(initialState)}
    </output>
  ),
}));

afterEach(cleanup);

describe("EssayRenderer", () => {
  it("renders the canonical plain-text answer when rich text is absent", () => {
    const plainText =
      "A Rust value has one owner and borrowing does not transfer ownership.";
    const answerState: RendererAnswerState = {
      selectedOptionIds: [],
      textAnswers: { main: "", main_plain: plainText },
      categorizations: {},
      ordering: [],
    };

    render(
      <EssayRenderer
        entry={{ ...createEssayEntry("Explain ownership."), showWordCount: true }}
        answerState={answerState}
        onAnswerChange={vi.fn()}
        disabled
      />,
    );

    const state = JSON.parse(
      screen.getByTestId("essay-answer-state").textContent ?? "null",
    );
    expect(state.root.children[0].children[0].text).toBe(plainText);
    expect(screen.getByText("12 words")).toBeVisible();
  });
});
