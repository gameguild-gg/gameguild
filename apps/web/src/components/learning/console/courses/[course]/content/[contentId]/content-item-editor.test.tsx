import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ContentItemEditor } from "./content-item-editor";
import {
  createAssessment,
  deleteAssessment,
  restoreAssessment,
  updateContent,
} from "@/lib/learning/actions";
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
  createAssessment: vi.fn(),
  deleteAssessment: vi.fn(),
  restoreAssessment: vi.fn(),
}));

const putCodingDefinitionMock = vi.hoisted(() => ({
  putCodingDefinition: vi.fn(),
}));

vi.mock("@/lib/emception/put-coding-definition", () => ({
  putCodingDefinition: putCodingDefinitionMock.putCodingDefinition,
}));

vi.mock("@game-guild/lexical-surface", () => ({
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
  gradingConfig: null,
  content: null,
  jsonBody: null,
  settings: { isRequired: true },
  lessonFormat: null,
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
} satisfies ContentItemDetail;

describe("ContentItemEditor", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(createAssessment).mockResolvedValue({
      success: true,
      data: { id: "new-asmnt" },
    });
    vi.mocked(deleteAssessment).mockResolvedValue({ success: true, data: null });
    vi.mocked(restoreAssessment).mockResolvedValue({ success: true, data: null });
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
      "/workspace/learning/courses/course-1/content",
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

  // ── Code content: coding-tests bridge ──

  it("enables Configure Coding Tests and bridges to the editor route when a linked AutoGraded assessment exists", async () => {
    const user = userEvent.setup();

    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-1"
        linkedAssessmentGradingMethods="AutoGraded"
      />,
    );

    const configure = screen.getByRole("button", {
      name: /configure coding tests/i,
    });
    expect(configure).not.toBeDisabled();

    await user.click(configure);

    await waitFor(() => {
      expect(routerMocks.push).toHaveBeenCalledWith(
        "/workspace/learning/courses/course-1/assessments/asmnt-1/coding-definition",
      );
    });
  });

  it("shows Edit Coding Tests summary when an existing coding definition is provided", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-1"
        linkedAssessmentGradingMethods="AutoGraded"
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
    expect(screen.getByText(/language: cpp/i)).toBeInTheDocument();
    expect(screen.getByText(/test cases: 2/i)).toBeInTheDocument();
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

// ── Task 7: Graded toggle (create / soft-delete / restore) ──

const codeItem = {
  ...assignmentItem,
  id: "content-code",
  type: "Code",
  title: "Sum two numbers",
} satisfies ContentItemDetail;

const projectItem = {
  ...assignmentItem,
  id: "content-proj",
  type: "Project",
  title: "Capstone project",
} satisfies ContentItemDetail;

describe("ContentItemEditor — Graded toggle (Task 7)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(createAssessment).mockResolvedValue({
      success: true,
      data: { id: "new-asmnt" },
    });
    vi.mocked(deleteAssessment).mockResolvedValue({ success: true, data: null });
    vi.mocked(restoreAssessment).mockResolvedValue({ success: true, data: null });
  });

  it("renders the Graded toggle for Code content; coding-tests panel only appears when an AutoGraded assessment is linked", () => {
    // No linked assessment → switch visible, coding-tests panel absent.
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(
      screen.getByRole("switch", { name: /^graded$/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByTestId("coding-tests-section"),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/not yet available/i)).not.toBeInTheDocument();
  });

  it("renders the Graded switch and coding-tests panel together for Code content with a linked AutoGraded assessment", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-1"
        linkedAssessmentGradingMethods="AutoGraded"
      />,
    );

    expect(
      screen.getByRole("switch", { name: /^graded$/i }),
    ).toBeInTheDocument();
    expect(screen.getByTestId("coding-tests-section")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /configure coding tests/i }),
    ).toBeInTheDocument();
  });

  it("does not render the Graded toggle for Lesson content", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownBody}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(
      screen.queryByRole("switch", { name: /^graded$/i }),
    ).not.toBeInTheDocument();
    expect(screen.queryByTestId("graded-section")).not.toBeInTheDocument();
  });

  it("renders the Graded toggle for Assignment, Questionnaire, and Project", () => {
    const { rerender } = render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
      />,
    );
    expect(
      screen.getByRole("switch", { name: /^graded$/i }),
    ).toBeInTheDocument();

    rerender(
      <ContentItemEditor
        courseId="course-1"
        item={item /* Questionnaire */}
        courseTitle="Advanced Game AI"
      />,
    );
    expect(
      screen.getByRole("switch", { name: /^graded$/i }),
    ).toBeInTheDocument();

    rerender(
      <ContentItemEditor
        courseId="course-1"
        item={projectItem}
        courseTitle="Advanced Game AI"
      />,
    );
    expect(
      screen.getByRole("switch", { name: /^graded$/i }),
    ).toBeInTheDocument();
  });

  it("creates an assessment when toggled ON with no existing link", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("switch", { name: /^graded$/i }));

    await waitFor(() => {
      expect(createAssessment).toHaveBeenCalledWith(
        expect.objectContaining({
          courseId: "course-1",
          contentId: "content-code",
          type: "Assignment",
          submissionModalities: "Code",
          gradingMethods: "AutoGraded,InstructorGraded",
        }),
      );
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
  });

  it("restores a recently soft-deleted assessment when toggled back ON", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-existing"
      />,
    );

    // Toggle OFF (opens confirm) → confirm → deleteAssessment.
    await user.click(screen.getByRole("switch", { name: /^graded$/i }));
    const dialog = await screen.findByRole("alertdialog");
    await user.click(
      within(dialog).getByRole("button", { name: /remove grading/i }),
    );

    await waitFor(() => {
      expect(deleteAssessment).toHaveBeenCalledWith("course-1", "asmnt-existing");
    });

    const gradedSwitch = screen.getByRole("switch", { name: /^graded$/i });
    await waitFor(() => {
      expect(gradedSwitch).not.toBeDisabled();
      expect(gradedSwitch).not.toBeChecked();
    });

    // Toggle ON again → restore (NOT create).
    await user.click(gradedSwitch);

    await waitFor(() => {
      expect(restoreAssessment).toHaveBeenCalledWith("course-1", "asmnt-existing");
    });
    expect(createAssessment).not.toHaveBeenCalled();
  });

  it("soft-deletes the linked assessment on confirm after toggling OFF", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-2"
      />,
    );

    await user.click(screen.getByRole("switch", { name: /^graded$/i }));
    const dialog = await screen.findByRole("alertdialog");
    await user.click(
      within(dialog).getByRole("button", { name: /remove grading/i }),
    );

    await waitFor(() => {
      expect(deleteAssessment).toHaveBeenCalledWith("course-1", "asmnt-2");
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
  });

  it("does not soft-delete when the OFF confirm is cancelled", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessmentId="asmnt-3"
      />,
    );

    await user.click(screen.getByRole("switch", { name: /^graded$/i }));
    const dialog = await screen.findByRole("alertdialog");
    await user.click(within(dialog).getByRole("button", { name: /^cancel$/i }));

    expect(deleteAssessment).not.toHaveBeenCalled();
    expect(restoreAssessment).not.toHaveBeenCalled();
    expect(createAssessment).not.toHaveBeenCalled();
    // Switch state unchanged — still ON because no mutation ran.
    expect(
      screen.getByRole("switch", { name: /^graded$/i }),
    ).toBeChecked();
  });
});
