import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LexicalLessonRenderer } from "./lexical-lesson-renderer";

const mocks = vi.hoisted(() => ({
  surface: vi.fn(),
}));

vi.mock("@game-guild/lexical-surface", () => ({
  LexicalSurface: (props: Record<string, unknown>) => {
    mocks.surface(props);
    return <div aria-label="Rendered Lexical lesson" />;
  },
}));

vi.mock(
  "@/components/block-content-editor/lexical-surface-viewer-adapters",
  () => ({
    lexicalSurfaceViewerAdapters: { assets: {} },
  }),
);

const state = {
  root: {
    children: [],
    direction: null,
    format: "",
    indent: 0,
    type: "root",
    version: 1,
  },
};

describe("LexicalLessonRenderer", () => {
  beforeEach(() => {
    mocks.surface.mockClear();
  });

  it("renders serialized content through LexicalSurface in read-only mode", () => {
    render(
      <LexicalLessonRenderer
        content={JSON.stringify(state)}
        itemId="lesson-1"
      />,
    );

    expect(
      screen.getByLabelText("Rendered Lexical lesson"),
    ).toBeInTheDocument();
    expect(mocks.surface).toHaveBeenCalledWith(
      expect.objectContaining({
        initialState: state,
        readOnly: true,
        features: { pageLayout: false },
      }),
    );
  });

  it("rejects content that is not a serialized editor state", () => {
    render(<LexicalLessonRenderer content="not-json" itemId="lesson-1" />);

    expect(
      screen.getByText("This Lexical lesson has no published content."),
    ).toBeInTheDocument();
    expect(mocks.surface).not.toHaveBeenCalled();
  });
});
