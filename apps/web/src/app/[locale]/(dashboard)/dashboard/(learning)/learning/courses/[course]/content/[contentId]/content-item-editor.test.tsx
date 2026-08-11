import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ContentItemEditor } from "./content-item-editor";
import { updateContent } from "@/lib/learning/actions";
import type { ContentItemDetail } from "@/lib/learning/types";

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
}));

vi.mock("@/lib/learning/actions", () => ({
  updateContent: vi.fn(),
}));

const putCodingDefinitionMock = vi.hoisted(() => ({
  putCodingDefinition: vi.fn(),
}));

vi.mock("@/lib/emception/put-coding-definition", () => ({
  putCodingDefinition: putCodingDefinitionMock.putCodingDefinition,
}));

vi.mock("@/components/block-content-editor/lexical-surface", () => ({
  LexicalSurface: ({ accessibleLabel }: { accessibleLabel?: string }) => (
    <textarea aria-label={accessibleLabel ?? "Body"} readOnly />
  ),
}));

vi.mock("./lesson-code-editor", () => ({
  LessonCodeEditor: ({
    initialValue,
    placeholder,
    onChange,
  }: {
    initialValue: string;
    placeholder?: string;
    onChange: (value: string) => void;
  }) => (
    <textarea
      aria-label="Body"
      defaultValue={initialValue}
      placeholder={placeholder}
      onChange={(event) => onChange(event.currentTarget.value)}
    />
  ),
}));

vi.mock(
  "@/components/block-content-editor/engines/blocks/block-array-editor",
  () => ({
    BlockArrayEditor: ({ blocks }: { blocks: unknown[] }) => (
      <div data-testid="block-array-editor">Blocks: {blocks.length}</div>
    ),
  }),
);

const item = {
  id: "content-1",
  parentId: "module-1",
  order: 1,
  type: "Questionnaire",
  title: "Intro quiz",
  description: "First knowledge check.",
  status: "published",
  duration: 20,
  metadata: {},
  gradingMethod: null,
  maxPoints: null,
  gradingConfig: null,
  content: null,
  jsonBody: { order: [], blocks: {} },
  settings: { isRequired: true },
  lessonFormat: null,
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
} satisfies ContentItemDetail;

const quizItemWithBlock = {
  ...item,
  id: "content-2",
  jsonBody: {
    order: [["1", "quiz"]],
    blocks: {
      "1": {
        type: "TRUE_FALSE",
        question: "Stored question",
        correctAnswer: true,
      },
    },
  },
} satisfies ContentItemDetail;

const lessonItemMarkdownEmpty = {
  id: "lesson-1",
  parentId: "module-1",
  order: 2,
  type: "Lesson",
  title: "Intro lesson",
  description: "Markdown-format lesson.",
  status: "published",
  duration: 15,
  metadata: {},
  gradingMethod: null,
  maxPoints: null,
  gradingConfig: null,
  content: "",
  jsonBody: null,
  settings: { isRequired: true },
  lessonFormat: null,
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
} satisfies ContentItemDetail;

const lessonItemMarkdownBody = {
  id: "lesson-2",
  parentId: "module-1",
  order: 3,
  type: "Lesson",
  title: "Markdown lesson",
  description: "Has a Markdown body.",
  status: "published",
  duration: 15,
  metadata: {},
  gradingMethod: null,
  maxPoints: null,
  gradingConfig: null,
  content: "# existing markdown",
  jsonBody: null,
  settings: { isRequired: true },
  lessonFormat: "Markdown",
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
} satisfies ContentItemDetail;

// Lexical lesson: structured state lives in jsonBody. content stays empty.
// Minimal valid SerializedEditorState: root + empty children.
const lessonItemLexical = {
  id: "lesson-3",
  parentId: "module-1",
  order: 4,
  type: "Lesson",
  title: "Lexical lesson",
  description: "Has a Lexical body.",
  status: "published",
  duration: 15,
  metadata: {},
  gradingMethod: null,
  maxPoints: null,
  gradingConfig: null,
  content: "",
  jsonBody: {
    root: {
      type: "root",
      children: [],
      direction: null,
      format: "",
      indent: 0,
      version: 1,
    },
  },
  settings: { isRequired: true },
  lessonFormat: "Lexical",
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
} satisfies ContentItemDetail;

describe("ContentItemEditor", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: null });
  });

  it("renders quiz-publication copy and normalizes questionnaire to quiz", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(
      screen.getByRole("heading", { name: "Intro quiz" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Advanced Game AI")).toBeInTheDocument();
    expect(screen.getByText("Quiz")).toBeInTheDocument();
    expect(screen.getByText("Quiz content")).toBeInTheDocument();
    expect(screen.getByText("Quiz publication")).toBeInTheDocument();
    expect(
      screen.getByText(
        /Public course landing-page visibility is managed in Listing/i,
      ),
    ).toBeInTheDocument();
  });

  it("loads quiz blocks from structured jsonBody", async () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={quizItemWithBlock}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(await screen.findByText("Blocks: 1")).toBeInTheDocument();
  });

  it("validates title before updating lesson content", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.clear(screen.getByLabelText(/^title$/i));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    expect(await screen.findByText("Title is required.")).toBeInTheDocument();
    expect(updateContent).not.toHaveBeenCalled();
  });

  it("saves edited quiz metadata and structured jsonBody, then refreshes the dashboard route", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.clear(screen.getByLabelText(/^title$/i));
    await user.type(screen.getByLabelText(/^title$/i), "Updated quiz");
    fireEvent.change(screen.getByLabelText(/description/i), {
      target: { value: "Updated description." },
    });
    fireEvent.change(screen.getByLabelText(/estimated minutes/i), {
      target: { value: "35" },
    });

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith({
        courseId: "course-1",
        contentId: "content-1",
        title: "Updated quiz",
        description: "Updated description.",
        body: undefined,
        jsonBody: item.jsonBody,
        visibility: "Public",
        isRequired: true,
        estimatedMinutes: 35,
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
  });

  it("saves quiz grading metadata inside structured jsonBody", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("switch", { name: /grading/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          courseId: "course-1",
          contentId: "content-1",
          title: "Intro quiz",
        }),
      );
    });

    const jsonBody = vi.mocked(updateContent).mock.calls[0]![0].jsonBody as {
      grading?: {
        enabled?: boolean;
        outcome?: { uses?: string[]; gradebook?: unknown };
        score?: { maxScore?: number };
      };
    };
    expect(vi.mocked(updateContent).mock.calls[0]![0].body).toBeUndefined();
    expect(jsonBody.grading).toMatchObject({
      enabled: true,
      outcome: {
        uses: ["feedback"],
        gradebook: null,
      },
      score: {
        maxScore: 1,
      },
    });
  });

  it("shows update errors and routes cancel back to the course content deterministically", async () => {
    const user = userEvent.setup();
    vi.mocked(updateContent).mockResolvedValueOnce({
      success: false,
      error: "Bad Request",
    });

    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));
    expect(await screen.findByText("Bad Request")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /^cancel$/i }));
    expect(routerMocks.push).toHaveBeenCalledWith(
      "/dashboard/learning/courses/course-1/content",
    );
    expect(routerMocks.back).not.toHaveBeenCalled();
  });

  it("renders the lesson format as read-only for Lesson items and defaults to Markdown when body is empty", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownEmpty}
        courseTitle="Advanced Game AI"
      />,
    );

    const formatInput = screen.getByLabelText(/lesson format/i);
    expect(formatInput).toHaveValue("Markdown");
    expect(formatInput).toHaveAttribute("readonly");
  });

  it("does not offer lesson format changes after creation", () => {
    const confirmSpy = vi.spyOn(window, "confirm");
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownBody}
        courseTitle="Advanced Game AI"
      />,
    );

    const formatInput = screen.getByLabelText(/lesson format/i);
    expect(formatInput).toHaveValue("Markdown");
    expect(formatInput).toHaveAttribute("readonly");
    expect(
      screen.queryByRole("option", { name: /^html$/i }),
    ).not.toBeInTheDocument();
    expect(confirmSpy).not.toHaveBeenCalled();

    confirmSpy.mockRestore();
  });

  it("saves a Markdown lesson with lessonFormat and seeded body passed to updateContent", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownBody}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      // body round-trips as a string; jsonBody explicitly undefined for text formats.
      expect(updateContent).toHaveBeenCalledWith({
        courseId: "course-1",
        contentId: "lesson-2",
        title: "Markdown lesson",
        description: "Has a Markdown body.",
        body: "# existing markdown",
        jsonBody: undefined,
        visibility: "Public",
        isRequired: true,
        estimatedMinutes: 15,
        lessonFormat: "Markdown",
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
  });

  it("saves a Lexical lesson with jsonBody (no body) forwarded to updateContent", async () => {
    // LexicalSurface is lazy + heavy; under jsdom it does not hydrate, so the
    // editor's onChange never fires and editorStateRef stays at the seeded
    // initial value derived from item.jsonBody. This test asserts the load→save
    // wiring: the seeded jsonBody object is forwarded verbatim, body stays
    // undefined, and lessonFormat is "Lexical".
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemLexical}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith({
        courseId: "course-1",
        contentId: "lesson-3",
        title: "Lexical lesson",
        description: "Has a Lexical body.",
        body: undefined,
        jsonBody: lessonItemLexical.jsonBody,
        visibility: "Public",
        isRequired: true,
        estimatedMinutes: 15,
        lessonFormat: "Lexical",
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
  });

  it("shows a Preview toggle on lessons that swaps the body editor for the learner renderer", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownBody}
        courseTitle="Advanced Game AI"
      />,
    );

    const previewButton = screen.getByRole("button", { name: /preview/i });
    expect(previewButton).toBeInTheDocument();
    expect(screen.queryByTestId("lesson-preview")).not.toBeInTheDocument();

    // Body seeded from item.content; renderer sees it without typing into Monaco.
    await user.click(previewButton);

    expect(screen.getByRole("button", { name: /^edit$/i })).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /^preview$/i }),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("lesson-preview")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /^edit$/i }));
    expect(
      screen.getByRole("button", { name: /preview/i }),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("lesson-preview")).not.toBeInTheDocument();
  });

  it("does not show a Preview toggle on non-lesson items", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(
      screen.queryByRole("button", { name: /preview/i }),
    ).not.toBeInTheDocument();
  });

  // ── Assignment / Project: coding-assignment bridge ──

  const assignmentItem = {
    id: "content-asn",
    parentId: "module-1",
    order: 5,
    type: "Assignment",
    title: "Hello world coding task",
    description: "Echo stdin to stdout.",
    status: "published",
    duration: 60,
    metadata: {},
    gradingMethod: null,
    maxPoints: null,
    gradingConfig: null,
    content: null,
    jsonBody: null,
    settings: { isRequired: true },
    lessonFormat: null,
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-02T00:00:00.000Z",
  } satisfies ContentItemDetail;

  it("renders the Coding Assignment panel for Assignment content (no linked assessment) and not the 'not yet available' fallback", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(
      screen.queryByText(/not yet available/i),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: /hello world coding task/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /configure coding assignment/i }),
    ).toBeInTheDocument();
  });

  it("enables Configure Coding Assignment and bridges to the editor route when a linked assessment exists", async () => {
    const user = userEvent.setup();
    putCodingDefinitionMock.putCodingDefinition.mockResolvedValue({
      success: true,
    });

    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-1"
      />,
    );

    const configure = screen.getByRole("button", {
      name: /configure coding assignment/i,
    });
    expect(configure).not.toBeDisabled();

    await user.click(configure);

    await waitFor(() => {
      expect(putCodingDefinitionMock.putCodingDefinition).toHaveBeenCalledWith(
        "asmnt-1",
        expect.objectContaining({
          kind: "coding",
          language: "cpp",
          maxScore: 100,
          passingScore: 60,
        }),
        "course-1",
      );
    });
    expect(routerMocks.push).toHaveBeenCalledWith(
      "/dashboard/learning/courses/course-1/assessments/asmnt-1/coding-definition",
    );
  });

  it("shows Edit Coding Tests summary when an existing coding definition is provided", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-1"
        initialCodingDefinition={{
          kind: "coding",
          language: "cpp",
          workspaceConfig: { id: "x" },
          testPlan: { cases: [{ kind: "stdio" }, { kind: "stdio" }] },
          maxScore: 100,
          passingScore: 60,
        }}
      />,
    );

    expect(
      screen.getByRole("button", { name: /edit coding tests/i }),
    ).toBeInTheDocument();
    expect(screen.getByText(/cpp/i)).toBeInTheDocument();
    expect(screen.getByText(/2 test cases/i)).toBeInTheDocument();
  });

  it("still shows the 'not yet available' fallback for other unhandled types (Discussion)", () => {
    const discussionItem = {
      ...assignmentItem,
      id: "content-disc",
      type: "Discussion",
      title: "Week 1 chat",
    } satisfies ContentItemDetail;

    render(
      <ContentItemEditor
        courseId="course-1"
        item={discussionItem}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(screen.getByText(/not yet available/i)).toBeInTheDocument();
  });
});
