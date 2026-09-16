import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  disableGrading: vi.fn(),
  enableGrading: vi.fn(),
  itemsToDocument: vi.fn(),
  parseDocument: vi.fn(),
  readGrading: vi.fn(),
  serializeDocument: vi.fn(),
  sumPoints: vi.fn(),
  toItems: vi.fn(),
  updateGrading: vi.fn(),
}));

vi.mock("@game-guild/grading", () => ({
  sumGradedItemPoints: mocks.sumPoints,
}));

vi.mock("@game-guild/quiz-content", () => ({
  disableQuizContentGrading: mocks.disableGrading,
  enableQuizContentGrading: mocks.enableGrading,
  parseQuizContentDocument: mocks.parseDocument,
  quizContentItemsToDocument: mocks.itemsToDocument,
  quizDocumentToContentItems: mocks.toItems,
  readQuizContentGrading: mocks.readGrading,
  serializeQuizContentDocument: mocks.serializeDocument,
  updateQuizContentGrading: mocks.updateGrading,
}));

vi.mock("@game-guild/quiz-surface/editor", () => ({
  QuizCollectionEditor: ({
    items,
    onChange,
    readOnly = false,
    submissionMode,
  }: {
    items: unknown[];
    onChange: (items: unknown[]) => void;
    readOnly?: boolean;
    submissionMode: string;
  }) => (
    <div
      data-testid="quiz-collection-editor"
      data-read-only={String(readOnly)}
      data-submission-mode={submissionMode}
    >
      <span>{items.length} quiz items</span>
      <button type="button" onClick={() => onChange([{ id: "question-2" }])}>
        Replace quiz items
      </button>
    </div>
  ),
}));

import { QuizContentEditor } from "./quiz-content-editor";

type TestDocument = {
  items: Array<{ id: string }>;
  grading: {
    enabled: boolean;
    items: Record<string, unknown>;
    score: { maxScore: number; passingScore?: number };
  };
};

const disabledDocument: TestDocument = {
  items: [{ id: "question-1" }],
  grading: {
    enabled: false,
    items: {},
    score: { maxScore: 10 },
  },
};

function renderEditor(
  document: TestDocument = disabledDocument,
  mode?: "edit" | "preview",
) {
  const onChange = vi.fn();
  render(
    <QuizContentEditor
      initialContent={document as never}
      onChange={onChange}
      mode={mode}
    />,
  );
  return { onChange };
}

describe("QuizContentEditor", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.parseDocument.mockImplementation((content) => ({
      document: content,
    }));
    mocks.toItems.mockImplementation(
      (document: TestDocument) => document.items,
    );
    mocks.readGrading.mockImplementation(
      (document: TestDocument) => document.grading,
    );
    mocks.sumPoints.mockReturnValue(7);
    mocks.serializeDocument.mockImplementation((document) => document);
    mocks.itemsToDocument.mockImplementation(({ items, grading }) => ({
      items,
      grading,
    }));
    mocks.enableGrading.mockImplementation((document: TestDocument) => ({
      ...document,
      grading: { ...document.grading, enabled: true },
    }));
    mocks.disableGrading.mockImplementation((document: TestDocument) => ({
      ...document,
      grading: { ...document.grading, enabled: false },
    }));
    mocks.updateGrading.mockImplementation(
      (
        document: TestDocument,
        updater: (current: TestDocument["grading"]) => TestDocument["grading"],
      ) => ({
        ...document,
        grading: updater(document.grading),
      }),
    );
  });

  it("renders an ungraded quiz in local-practice edit mode", () => {
    renderEditor();

    expect(screen.getByText("Off")).toBeInTheDocument();
    expect(screen.queryByLabelText("Max score")).not.toBeInTheDocument();
    expect(screen.getByTestId("quiz-collection-editor")).toHaveAttribute(
      "data-submission-mode",
      "local-practice",
    );
    expect(screen.getByTestId("quiz-collection-editor")).toHaveAttribute(
      "data-read-only",
      "false",
    );
  });

  it("renders both graded and practice previews as read-only", () => {
    const { unmount } = render(
      <QuizContentEditor
        initialContent={disabledDocument as never}
        onChange={vi.fn()}
        mode="preview"
      />,
    );
    expect(screen.getByTestId("quiz-collection-editor")).toHaveAttribute(
      "data-submission-mode",
      "local-practice",
    );
    expect(screen.getByTestId("quiz-collection-editor")).toHaveAttribute(
      "data-read-only",
      "true",
    );

    unmount();
    renderEditor(
      {
        ...disabledDocument,
        grading: { ...disabledDocument.grading, enabled: true },
      },
      "preview",
    );
    expect(screen.getByTestId("quiz-collection-editor")).toHaveAttribute(
      "data-submission-mode",
      "server-graded",
    );
  });

  it("enables and disables server grading", () => {
    const { onChange } = renderEditor();
    const gradingSwitch = screen.getByRole("switch", { name: "Grading" });

    fireEvent.click(gradingSwitch);
    expect(mocks.enableGrading).toHaveBeenCalledWith(disabledDocument);
    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({ enabled: true }),
      }),
    );
    expect(screen.getByText("0 items")).toBeInTheDocument();
    expect(screen.getByText("7 configured pts")).toBeInTheDocument();
    expect(screen.getByText("Assessment")).toBeInTheDocument();

    fireEvent.click(gradingSwitch);
    expect(mocks.disableGrading).toHaveBeenCalled();
    expect(screen.getByText("Off")).toBeInTheDocument();
  });

  it("rebuilds the document when quiz items change", () => {
    const { onChange } = renderEditor();

    fireEvent.click(screen.getByRole("button", { name: "Replace quiz items" }));

    expect(mocks.itemsToDocument).toHaveBeenCalledWith({
      items: [{ id: "question-2" }],
      grading: disabledDocument.grading,
    });
    expect(onChange).toHaveBeenCalledWith(
      expect.objectContaining({ items: [{ id: "question-2" }] }),
    );
  });

  it("normalizes max score and clamps an existing passing score", () => {
    const { onChange } = renderEditor({
      ...disabledDocument,
      grading: {
        enabled: true,
        items: { one: {}, two: {} },
        score: { maxScore: 10, passingScore: 8 },
      },
    });

    fireEvent.change(screen.getByLabelText("Max score"), {
      target: { value: "5" },
    });
    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({
          score: { maxScore: 5, passingScore: 5 },
        }),
      }),
    );

    fireEvent.change(screen.getByLabelText("Max score"), {
      target: { value: "0" },
    });
    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({
          score: { maxScore: 1, passingScore: 1 },
        }),
      }),
    );
  });

  it("keeps an undefined passing score while max score changes", () => {
    const { onChange } = renderEditor({
      ...disabledDocument,
      grading: { ...disabledDocument.grading, enabled: true },
    });

    fireEvent.change(screen.getByLabelText("Max score"), {
      target: { value: "20" },
    });

    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({
          score: { maxScore: 20, passingScore: undefined },
        }),
      }),
    );
  });

  it("clears, clamps, and normalizes passing scores", () => {
    const { onChange } = renderEditor({
      ...disabledDocument,
      grading: {
        ...disabledDocument.grading,
        enabled: true,
        score: { maxScore: 10, passingScore: 6 },
      },
    });
    const passingScore = screen.getByLabelText("Passing score");

    fireEvent.change(passingScore, { target: { value: "" } });
    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({
          score: { maxScore: 10, passingScore: undefined },
        }),
      }),
    );

    fireEvent.change(passingScore, { target: { value: "20" } });
    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({
          score: { maxScore: 10, passingScore: 10 },
        }),
      }),
    );

    fireEvent.change(passingScore, { target: { value: "-2" } });
    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({
          score: { maxScore: 10, passingScore: 0 },
        }),
      }),
    );

    passingScore.setAttribute("type", "text");
    fireEvent.change(passingScore, { target: { value: "invalid" } });
    expect(onChange).toHaveBeenLastCalledWith(
      expect.objectContaining({
        grading: expect.objectContaining({
          score: { maxScore: 10, passingScore: 0 },
        }),
      }),
    );
  });
});
