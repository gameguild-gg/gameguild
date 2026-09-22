import "@testing-library/jest-dom/vitest";
import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { renderToString } from "react-dom/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  params: { locale: "en-US" } as { locale?: string },
  theme: "dark",
  push: vi.fn(),
  replace: vi.fn(),
  refresh: vi.fn(),
  saveDraft: vi.fn(),
  getDraft: vi.fn(),
  publishDraft: vi.fn(),
  getEntitlement: vi.fn(),
  getConversations: vi.fn(),
  createRun: vi.fn(),
  getRun: vi.fn(),
  cancelRun: vi.fn(),
  applyProposal: vi.fn(),
  discardProposal: vi.fn(),
  prepareAssets: vi.fn(),
  createAssessment: vi.fn(),
  deleteAssessment: vi.fn(),
  restoreAssessment: vi.fn(),
  assetRepository: {},
  codeEditorProps: undefined as
    | {
        language: string;
        placeholder?: string;
        onChange: (value: string) => void;
        onCursorOffsetChange: (offset: number) => void;
      }
    | undefined,
  quizEditorModes: [] as string[],
  diffEditorProps: undefined as
    | {
        language: string;
        theme: string;
        options: { renderSideBySide: boolean };
      }
    | undefined,
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mocks.push,
    replace: mocks.replace,
    refresh: mocks.refresh,
  }),
  useParams: () => mocks.params,
}));
vi.mock("@/lib/learning/use-learning-base", () => ({
  useLearningBase: () => "/workspace/learning",
}));
vi.mock("@/lib/learning/authoring", () => ({
  saveAuthoringDraft: mocks.saveDraft,
  getAuthoringDraft: mocks.getDraft,
  publishAuthoringDraft: mocks.publishDraft,
  getAiEntitlement: mocks.getEntitlement,
  getAiConversations: mocks.getConversations,
  createAiAuthoringRun: mocks.createRun,
  getAiAuthoringRun: mocks.getRun,
  cancelAiAuthoringRun: mocks.cancelRun,
  applyAiProposal: mocks.applyProposal,
  discardAiProposal: mocks.discardProposal,
}));
vi.mock("@/lib/learning/assets/learning-asset-repository", () => ({
  getLearningAssetRepository: () => mocks.assetRepository,
}));
vi.mock("@/lib/learning/assets/prepare-authoring-assets", () => ({
  prepareAuthoringAssets: mocks.prepareAssets,
}));
vi.mock("@/lib/learning/actions", () => ({
  createAssessment: mocks.createAssessment,
  deleteAssessment: mocks.deleteAssessment,
  restoreAssessment: mocks.restoreAssessment,
}));
vi.mock("@/components/learning/learner-lesson-renderer", () => ({
  LearnerLessonRenderer: ({ content }: { content: unknown }) => (
    <div data-testid="learner-renderer">
      {typeof content === "string" ? content : JSON.stringify(content)}
    </div>
  ),
}));
vi.mock("@game-guild/ui/components/scroll-area", () => ({
  ScrollArea: ({ children, ...props }: ComponentProps<"div">) => (
    <div {...props}>{children}</div>
  ),
}));
vi.mock(
  "@/components/learning/console/courses/[course]/content/[contentId]/lesson-code-editor",
  () => ({
    LessonCodeEditor: (props: {
      initialValue: string;
      language: string;
      placeholder?: string;
      onChange: (value: string) => void;
      onCursorOffsetChange: (offset: number) => void;
    }) => {
      mocks.codeEditorProps = props;
      return (
        <textarea
          aria-label="Lesson body"
          defaultValue={props.initialValue}
          onChange={(event) => props.onChange(event.target.value)}
          onSelect={() => props.onCursorOffsetChange(4)}
        />
      );
    },
  }),
);
vi.mock(
  "@/components/learning/console/courses/[course]/content/[contentId]/lesson-content-editor",
  () => ({
    LessonContentEditor: ({
      onChange,
    }: {
      onChange: (value: Record<string, unknown>) => void;
    }) => (
      <button
        type="button"
        onClick={() => onChange({ root: { children: [] } })}
      >
        Lexical editor
      </button>
    ),
  }),
);
vi.mock(
  "@/components/learning/console/courses/[course]/content/[contentId]/lesson-video-editor",
  () => ({
    LessonVideoEditor: ({
      onChange,
    }: {
      onChange: (value: string) => void;
    }) => (
      <button
        type="button"
        onClick={() => onChange("https://cdn.example.test/changed.mp4")}
      >
        Video editor
      </button>
    ),
  }),
);
vi.mock(
  "@/components/learning/console/courses/[course]/content/[contentId]/lesson-external-link-editor",
  () => ({
    LessonExternalLinkEditor: ({
      onChange,
    }: {
      onChange: (value: string) => void;
    }) => (
      <button
        type="button"
        onClick={() => onChange("https://example.test/updated-article")}
      >
        External link editor
      </button>
    ),
  }),
);
vi.mock(
  "@/components/learning/console/courses/[course]/content/[contentId]/quiz-content-editor",
  () => ({
    QuizContentEditor: ({
      onChange,
      mode = "edit",
    }: {
      onChange: (value: Record<string, unknown>) => void;
      mode?: "edit" | "preview";
    }) => {
      mocks.quizEditorModes.push(mode);
      return mode === "preview" ? (
        <div data-testid="quiz-preview">Quiz preview</div>
      ) : (
        <button type="button" onClick={() => onChange({ questions: [] })}>
          Quiz editor
        </button>
      );
    },
  }),
);
vi.mock(
  "@/components/learning/console/courses/[course]/assessments/[assessmentId]/coding-definition/coding-definition-editor",
  () => ({
    CodingDefinitionEditor: ({
      embedded,
      onSaved,
    }: {
      embedded?: boolean;
      onSaved?: (content: Record<string, unknown>) => void;
    }) => (
      <button
        type="button"
        data-testid="coding-definition-editor"
        data-embedded={String(embedded)}
        onClick={() =>
          onSaved?.({
            Type: "coding-assignment",
            Version: 1,
            Environment: { Language: "cpp" },
            Data: { Files: {} },
            Tests: { Public: [], Private: [] },
            Grading: { MaxScore: 100 },
          })
        }
      >
        Coding assignment editor
      </button>
    ),
  }),
);
vi.mock("@/components/learning/authoring/coding-assignment-preview", () => ({
  CodingAssignmentPreview: () => (
    <div data-testid="coding-assignment-preview">Coding assignment preview</div>
  ),
}));
vi.mock("@monaco-editor/react", () => ({
  DiffEditor: (props: {
    language: string;
    theme: string;
    options: { renderSideBySide: boolean };
  }) => {
    mocks.diffEditorProps = props;
    return <div>Diff editor</div>;
  },
}));
vi.mock("next-themes", () => ({
  useTheme: () => ({ resolvedTheme: mocks.theme }),
}));

import {
  AuthoringLocalTime,
  LessonAuthoringWorkspace,
} from "./lesson-authoring-workspace";

describe("AuthoringLocalTime", () => {
  it("renders a deterministic placeholder during SSR", () => {
    expect(
      renderToString(<AuthoringLocalTime value="2026-09-10T12:00:00Z" />),
    ).toContain("—");
  });
});

const initialDraft = {
  id: "draft-1",
  programId: "course-1",
  contentId: "lesson-1",
  payload: {
    title: "Original lesson",
    slug: "original-lesson",
    description: "Description",
    type: "Lesson",
    body: "Original body",
    jsonBody: null,
    lessonFormat: "Markdown",
    activitySettings: null,
    isRequired: true,
    estimatedMinutes: 5,
    estimatedMinutesSource: "Manual",
    visibility: "Public",
  },
  basePublishedVersion: 3,
  revision: 1,
  eTag: '"draft-1-1"',
  lastEditedBy: "author-1",
  lastEditedAt: "2026-09-10T12:00:00Z",
} as const;

const item = {
  id: "lesson-1",
  title: "Original lesson",
  slug: "original-lesson",
  type: "Lesson",
  status: "draft",
  order: 1,
} as never;

const pendingProposal = {
  id: "proposal-1",
  runId: "run-1",
  kind: "ReplaceDocument",
  status: "Pending",
  baseDraftRevision: 1,
  originalContent: "Original body",
  proposedContent: "Improved body",
} as const;

const runningRun = {
  id: "run-1",
  conversationId: "conversation-1",
  status: "Running",
  errorCode: null,
  proposal: null,
  usage: {
    maximumEstimatedCost: 20,
    reservedCost: 20,
    settledCost: 0,
    releasedCost: 0,
    inputTokens: 0,
    outputTokens: 0,
  },
} as const;

const completedRun = {
  ...runningRun,
  status: "Completed",
  usage: {
    ...runningRun.usage,
    reservedCost: 0,
    settledCost: 3,
    releasedCost: 17,
    inputTokens: 11,
    outputTokens: 7,
  },
} as const;

function streamResponse(...frames: string[]) {
  const encoder = new TextEncoder();
  return new Response(
    new ReadableStream({
      start(controller) {
        for (const frame of frames)
          controller.enqueue(encoder.encode(`${frame}\n\n`));
        controller.close();
      },
    }),
    { status: 200 },
  );
}

async function flushCopilotRetries(iterations = 8) {
  for (let index = 0; index < iterations; index += 1) {
    for (let microtask = 0; microtask < 12; microtask += 1)
      await Promise.resolve();
    await vi.advanceTimersByTimeAsync(5000);
  }
  for (let microtask = 0; microtask < 12; microtask += 1)
    await Promise.resolve();
}

describe("LessonAuthoringWorkspace", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    window.sessionStorage.clear();
    mocks.params = { locale: "en-US" };
    mocks.theme = "dark";
    mocks.codeEditorProps = undefined;
    mocks.quizEditorModes = [];
    mocks.diffEditorProps = undefined;
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      value: vi.fn().mockReturnValue({ matches: true }),
    });
    mocks.saveDraft.mockImplementation(
      async (_courseId, _contentId, revision, payload) => ({
        success: true,
        data: { ...initialDraft, payload, revision: revision + 1 },
      }),
    );
    mocks.getDraft.mockResolvedValue({
      success: true,
      data: initialDraft,
    });
    mocks.publishDraft.mockImplementation(
      async (_courseId, _contentId, revision) => ({
        success: true,
        data: {
          draft: {
            ...initialDraft,
            revision: revision + 1,
            basePublishedVersion: 4,
          },
          publishedContent: { slug: initialDraft.payload.slug },
        },
      }),
    );
    mocks.getEntitlement.mockResolvedValue({
      success: true,
      data: {
        availableSoftCredits: 100,
        reservedSoftCredits: 0,
        settledSoftCredits: 0,
        currency: "SoftCoin",
      },
    });
    mocks.getConversations.mockResolvedValue({ success: true, data: [] });
    mocks.createRun.mockResolvedValue({
      success: false,
      error: "Not configured",
      status: 503,
    });
    mocks.getRun.mockResolvedValue({
      success: false,
      error: "Run not found",
      status: 404,
    });
    mocks.cancelRun.mockResolvedValue({
      success: false,
      error: "Unable to cancel",
      status: 409,
    });
    mocks.applyProposal.mockResolvedValue({
      success: false,
      error: "Unable to apply",
      status: 409,
    });
    mocks.discardProposal.mockResolvedValue({
      success: false,
      error: "Unable to discard",
      status: 409,
    });
    mocks.prepareAssets.mockResolvedValue({ assetUris: [], promotedUris: [] });
    mocks.createAssessment.mockResolvedValue({
      success: true,
      data: { id: "assessment-new" },
    });
    mocks.deleteAssessment.mockResolvedValue({ success: true, data: null });
    mocks.restoreAssessment.mockResolvedValue({ success: true, data: null });
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  function renderWorkspace(
    draft = initialDraft,
    options: {
      activeItem?: typeof item;
      curriculum?: (typeof item)[];
      linkedAssessment?: { id: string; slug: string; title: string } | null;
      initialCodingAssignment?: Record<string, unknown> | null;
    } = {},
  ) {
    const activeItem = options.activeItem ?? item;
    return render(
      <LessonAuthoringWorkspace
        courseId="course-1"
        courseSlug="course-slug"
        courseTitle="Course title"
        item={activeItem}
        curriculum={options.curriculum ?? [activeItem]}
        initialDraft={draft as never}
        linkedAssessment={options.linkedAssessment}
        initialCodingAssignment={options.initialCodingAssignment as never}
      />,
    );
  }

  it("autosaves edits with the current draft revision", async () => {
    renderWorkspace();

    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Updated lesson" },
    });
    await act(async () => vi.advanceTimersByTimeAsync(1200));

    expect(mocks.saveDraft).toHaveBeenCalledWith(
      "course-1",
      "lesson-1",
      1,
      expect.objectContaining({
        title: "Updated lesson",
        slug: "updated-lesson",
      }),
    );
    expect(screen.getByText("Saved")).toBeInTheDocument();
  });

  it("uses the learner renderer for preview and publishes the saved revision", async () => {
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: /preview/i }));
    expect(screen.getByTestId("learner-renderer")).toHaveTextContent(
      "Original body",
    );
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /publish changes/i }));
      await Promise.resolve();
    });

    expect(mocks.publishDraft).toHaveBeenCalledWith("course-1", "lesson-1", 1);
    expect(mocks.refresh).toHaveBeenCalled();
  });

  it("previews a questionnaire with the quiz renderer instead of the lesson renderer", () => {
    renderWorkspace({
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        type: "Questionnaire",
        lessonFormat: null,
        body: null,
        jsonBody: { schemaVersion: 1, order: [], blocks: {} },
      },
    } as never);

    fireEvent.click(screen.getByRole("button", { name: /preview/i }));

    expect(screen.getByTestId("quiz-preview")).toBeInTheDocument();
    expect(screen.queryByTestId("learner-renderer")).not.toBeInTheDocument();
    expect(mocks.quizEditorModes).toContain("preview");
  });

  it("opens the Copilot with the authenticated user's SoftCoin balance", async () => {
    renderWorkspace();

    fireEvent.click(screen.getAllByRole("button", { name: /copilot/i })[0]!);
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mocks.getEntitlement).toHaveBeenCalledWith("course-1", "lesson-1");
    expect(screen.getByText("100 SC")).toBeInTheDocument();
    expect(
      screen.getByText(/only actual token usage is charged/i),
    ).toBeInTheDocument();
  });

  it("limits video lessons to metadata-only Copilot proposals", async () => {
    const videoDraft = {
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        body: "https://cdn.example.test/original.mp4",
        lessonFormat: "Video",
      },
    } as const;
    mocks.createRun.mockResolvedValue({
      success: false,
      error: "Stopped after request capture",
      status: 400,
    });
    renderWorkspace(videoDraft);

    fireEvent.click(screen.getAllByRole("button", { name: /copilot/i })[0]!);
    fireEvent.change(screen.getByRole("textbox", { name: "Ask Copilot" }), {
      target: { value: "Improve the video description" },
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Send to Copilot" }));
      await Promise.resolve();
    });

    expect(screen.queryByText("Replace document")).not.toBeInTheDocument();
    expect(screen.getByText(/video media is protected/i)).toBeInTheDocument();
    expect(mocks.createRun).toHaveBeenCalledWith(
      "course-1",
      "lesson-1",
      expect.objectContaining({ proposalKind: "MetadataPatch" }),
    );
  });

  it("preserves local edits while resolving a concurrent draft conflict", async () => {
    mocks.saveDraft.mockResolvedValueOnce({
      success: false,
      error: "This lesson was updated by another author.",
      status: 409,
      currentRevision: 2,
    });
    mocks.getDraft.mockResolvedValue({
      success: true,
      data: {
        ...initialDraft,
        revision: 2,
        payload: { ...initialDraft.payload, title: "Latest team version" },
      },
    });
    renderWorkspace();

    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "My unsaved version" },
    });
    await act(async () => vi.advanceTimersByTimeAsync(1200));

    expect(screen.getByText("Conflict")).toBeInTheDocument();
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /load latest/i }));
      await Promise.resolve();
    });
    expect(screen.getByRole("textbox", { name: "Lesson title" })).toHaveValue(
      "Latest team version",
    );

    fireEvent.click(
      screen.getByRole("button", { name: /restore my changes/i }),
    );
    expect(screen.getByRole("textbox", { name: "Lesson title" })).toHaveValue(
      "My unsaved version",
    );
  });

  it("lets the author cancel a running Copilot request", async () => {
    const running = {
      id: "run-1",
      conversationId: "conversation-1",
      status: "Running",
      errorCode: null,
      usage: {
        maximumEstimatedCost: 20,
        reservedCost: 20,
        settledCost: 0,
        releasedCost: 0,
        inputTokens: 0,
        outputTokens: 0,
      },
    };
    window.sessionStorage.setItem(
      "authoring-ai-run:course-1:lesson-1",
      running.id,
    );
    mocks.getRun.mockResolvedValue({ success: true, data: running });
    mocks.cancelRun.mockResolvedValue({
      success: true,
      data: { ...running, status: "Cancelled", errorCode: "AI_CANCELLED" },
    });
    vi.stubGlobal(
      "fetch",
      vi.fn(
        (_url: string, init?: RequestInit) =>
          new Promise((_resolve, reject) => {
            init?.signal?.addEventListener("abort", () =>
              reject(new DOMException("Aborted", "AbortError")),
            );
          }),
      ),
    );

    renderWorkspace();
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Stop" }));
      await Promise.resolve();
    });

    expect(mocks.cancelRun).toHaveBeenCalledWith(
      "course-1",
      "lesson-1",
      "run-1",
    );
    expect(
      window.sessionStorage.getItem("authoring-ai-run:course-1:lesson-1"),
    ).toBeNull();
  });

  it("navigates the curriculum and student view with locale fallback", () => {
    const open = vi.spyOn(window, "open").mockReturnValue(null);
    mocks.params = {};
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: "Curriculum" }));
    expect(mocks.push).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-slug/content",
    );

    fireEvent.click(screen.getByRole("button", { name: "Open student view" }));
    expect(open).toHaveBeenCalledWith(
      "/en-US/learn/courses/course-slug/lessons/original-lesson",
      "_blank",
      "noopener,noreferrer",
    );
  });

  it.each([
    [
      "Questionnaire",
      "quiz-assessment",
      "/en-US/learn/courses/course-slug/activities/assessment-assessment-1",
    ],
    [
      "Code",
      "code-assessment",
      "/en-US/learn/courses/course-slug/activities/assessment-assessment-1",
    ],
  ])(
    "opens the linked %s assessment in student view",
    (type, assessmentSlug, expectedHref) => {
      const open = vi.spyOn(window, "open").mockReturnValue(null);
      render(
        <LessonAuthoringWorkspace
          courseId="course-1"
          courseSlug="course-slug"
          courseTitle="Course title"
          item={{ ...item, type } as never}
          curriculum={[{ ...item, type } as never]}
          initialDraft={
            {
              ...initialDraft,
              payload: { ...initialDraft.payload, type, lessonFormat: null },
            } as never
          }
          linkedAssessment={{ id: "assessment-1", slug: assessmentSlug }}
        />,
      );

      fireEvent.click(
        screen.getByRole("button", { name: "Open student view" }),
      );

      expect(open).toHaveBeenCalledWith(
        expectedHref,
        "_blank",
        "noopener,noreferrer",
      );
    },
  );

  it("supports desktop and mobile panel controls", async () => {
    const media = { matches: true };
    vi.mocked(window.matchMedia).mockImplementation(
      () => media as MediaQueryList,
    );
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: "Toggle curriculum" }));
    expect(
      screen.queryByRole("complementary", { name: "Course curriculum" }),
    ).not.toBeInTheDocument();
    media.matches = false;
    fireEvent.click(screen.getByRole("button", { name: "Toggle curriculum" }));
    expect(
      screen.getByRole("button", { name: "Close curriculum" }),
    ).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Close curriculum" }));

    media.matches = true;
    fireEvent.click(
      screen.getByRole("button", { name: "Toggle lesson panel" }),
    );
    expect(
      screen.queryByRole("complementary", { name: "Lesson settings" }),
    ).not.toBeInTheDocument();
    media.matches = false;
    fireEvent.click(
      screen.getByRole("button", { name: "Toggle lesson panel" }),
    );
    expect(
      screen.getByRole("button", { name: "Close lesson panel" }),
    ).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Close lesson panel" }));

    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(
      screen.getByRole("complementary", { name: "AI authoring copilot" }),
    ).toBeInTheDocument();
  });

  it("filters, sorts, labels, and opens curriculum entries", () => {
    const questionnaire = {
      ...item,
      id: "quiz-1",
      slug: "quiz",
      title: "Beta quiz",
      type: "Questionnaire",
      status: "published",
      order: 2,
    } as never;
    const code = {
      ...item,
      id: "code-1",
      slug: "code",
      title: "Alpha code",
      type: "Code",
      status: "draft",
      order: 1,
    } as never;
    renderWorkspace(initialDraft, { curriculum: [questionnaire, item, code] });

    expect(screen.getAllByLabelText("Published")).toHaveLength(1);
    expect(screen.getAllByLabelText("Draft")).toHaveLength(2);
    fireEvent.change(
      screen.getByPlaceholderText("Search lessons and quizzes"),
      {
        target: { value: " beta " },
      },
    );
    expect(screen.queryByText("Alpha code")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /Beta quiz/ }));
    expect(mocks.push).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-slug/content/quiz",
    );
  });

  it("edits markdown content and preserves a custom slug", async () => {
    const customSlugDraft = {
      ...initialDraft,
      payload: { ...initialDraft.payload, slug: "kept-custom-slug" },
    } as const;
    renderWorkspace(customSlugDraft);

    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "A renamed lesson" },
    });
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson body" }), {
      target: { value: "Changed body" },
    });
    fireEvent.select(screen.getByRole("textbox", { name: "Lesson body" }));
    await act(async () => vi.advanceTimersByTimeAsync(1200));

    expect(mocks.saveDraft).toHaveBeenLastCalledWith(
      "course-1",
      "lesson-1",
      1,
      expect.objectContaining({
        title: "A renamed lesson",
        slug: "kept-custom-slug",
        body: "Changed body",
      }),
    );
    expect(mocks.codeEditorProps?.language).toBe("markdown");
  });

  it.each([
    ["Html", "html", undefined],
    ["RevealJs", "markdown", "Separate slides with --- on its own line."],
  ] as const)(
    "configures the %s code editor",
    (lessonFormat, language, placeholder) => {
      renderWorkspace({
        ...initialDraft,
        payload: { ...initialDraft.payload, lessonFormat },
      } as never);

      expect(mocks.codeEditorProps).toEqual(
        expect.objectContaining({ language, placeholder }),
      );
    },
  );

  it("supports Lexical, video, and quiz editor changes", () => {
    const lexical = renderWorkspace({
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        lessonFormat: "Lexical",
        body: null,
        jsonBody: { root: { children: [{ type: "paragraph" }] } },
      },
    } as never);
    fireEvent.click(screen.getByRole("button", { name: "Lexical editor" }));
    lexical.unmount();

    const video = renderWorkspace({
      ...initialDraft,
      payload: { ...initialDraft.payload, lessonFormat: "Video", body: null },
    } as never);
    fireEvent.click(screen.getByRole("button", { name: "Video editor" }));
    video.unmount();

    renderWorkspace({
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        type: "Questionnaire",
        lessonFormat: null,
        body: null,
        jsonBody: { questions: [{ id: "q1" }] },
      },
    } as never);
    fireEvent.click(screen.getByRole("button", { name: "Quiz editor" }));
  });

  it("edits an external link lesson and keeps Copilot metadata-only", () => {
    const { unmount } = renderWorkspace({
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        lessonFormat: "ExternalLink",
        body: "https://example.test/article",
      },
    } as never);

    fireEvent.click(screen.getByRole("button", { name: "External link editor" }));
    unmount();

    const video = renderWorkspace({
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        lessonFormat: "ExternalLink",
        body: "https://example.test/article",
      },
    } as never);
    fireEvent.click(screen.getAllByRole("button", { name: /copilot/i })[0]!);
    expect(
      screen.getByText(/external resources are protected/i),
    ).toBeInTheDocument();
    expect(screen.queryByText("Replace document")).not.toBeInTheDocument();
    video.unmount();
  });

  it("updates lesson settings including automatic time estimation", () => {
    renderWorkspace();

    expect(screen.getByLabelText("Title", { selector: "input" })).toHaveValue(
      "Original lesson",
    );
    fireEvent.change(screen.getByLabelText("URL slug"), {
      target: { value: "  New URL  " },
    });
    fireEvent.blur(screen.getByLabelText("URL slug"));
    fireEvent.change(screen.getByLabelText("Description"), {
      target: { value: "New description" },
    });
    fireEvent.click(
      screen.getByRole("switch", { name: "Required for completion" }),
    );
    fireEvent.change(screen.getByLabelText("Estimated minutes"), {
      target: { value: "12" },
    });
    fireEvent.change(screen.getByLabelText("Estimated minutes"), {
      target: { value: "" },
    });

    expect(screen.getByLabelText("URL slug")).toHaveValue("new-url");
    expect(screen.getByLabelText("Description")).toHaveValue("New description");
    expect(screen.getByLabelText("Estimated minutes")).toHaveValue(null);
  });

  it("retries an offline asset preparation failure", async () => {
    mocks.prepareAssets.mockRejectedValueOnce(new Error("Asset upload failed"));
    renderWorkspace();
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Changed" },
    });
    await act(async () => vi.advanceTimersByTimeAsync(1200));

    expect(screen.getByText("Asset upload failed")).toBeInTheDocument();
    expect(screen.getByText("Offline")).toBeInTheDocument();
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /retry/i }));
      await Promise.resolve();
    });
    expect(mocks.saveDraft).toHaveBeenCalled();
  });

  it("shows controlled errors returned and thrown while saving", async () => {
    mocks.saveDraft.mockResolvedValueOnce({
      success: false,
      error: "Save unavailable",
      status: 503,
    });
    const first = renderWorkspace();
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Changed once" },
    });
    await act(async () => vi.advanceTimersByTimeAsync(1200));
    expect(screen.getByText("Save unavailable")).toBeInTheDocument();
    first.unmount();

    mocks.saveDraft.mockRejectedValueOnce("network down");
    renderWorkspace();
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Changed twice" },
    });
    await act(async () => vi.advanceTimersByTimeAsync(1200));
    expect(screen.getByText("Unable to save this draft.")).toBeInTheDocument();
  });

  it("handles latest-draft failures and discards the preserved conflict copy", async () => {
    mocks.saveDraft.mockResolvedValueOnce({
      success: false,
      error: "Conflict",
      status: 409,
    });
    mocks.getDraft.mockResolvedValueOnce({
      success: false,
      error: "Reload failed",
      status: 503,
    });
    renderWorkspace();
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "My version" },
    });
    await act(async () => vi.advanceTimersByTimeAsync(1200));
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /load latest/i }));
      await Promise.resolve();
    });
    expect(screen.getByText("Reload failed")).toBeInTheDocument();

    mocks.getDraft.mockResolvedValueOnce({
      success: true,
      data: { ...initialDraft, revision: 2 },
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /load latest/i }));
      await Promise.resolve();
    });
    fireEvent.click(screen.getByRole("button", { name: "Discard my copy" }));
    expect(
      screen.queryByRole("button", { name: "Discard my copy" }),
    ).not.toBeInTheDocument();
  });

  it("publishes an edited draft before refreshing the route", async () => {
    renderWorkspace();
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Ready to publish" },
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Publish changes" }));
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mocks.saveDraft).toHaveBeenCalledWith(
      "course-1",
      "lesson-1",
      1,
      expect.objectContaining({ title: "Ready to publish" }),
    );
    expect(mocks.publishDraft).toHaveBeenCalledWith("course-1", "lesson-1", 2);
    expect(mocks.refresh).toHaveBeenCalledOnce();
  });

  it("adopts the canonical published draft without scheduling another save", async () => {
    const canonicalPayload = {
      ...initialDraft.payload,
      description: "Normalized by the API",
    };
    mocks.publishDraft.mockResolvedValueOnce({
      success: true,
      data: {
        draft: {
          ...initialDraft,
          payload: canonicalPayload,
          revision: 2,
          basePublishedVersion: 4,
        },
        publishedContent: { slug: initialDraft.payload.slug },
      },
    });
    renderWorkspace();

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Publish changes" }));
      await Promise.resolve();
      await Promise.resolve();
    });
    await act(async () => vi.advanceTimersByTimeAsync(1200));

    expect(screen.getByLabelText("Description")).toHaveValue(
      "Normalized by the API",
    );
    expect(screen.getByText("4")).toBeInTheDocument();
    expect(mocks.saveDraft).not.toHaveBeenCalled();
  });

  it("uses the coding assignment authoring and learner preview for Code content", () => {
    const codingAssignment = {
      Type: "coding-assignment",
      Version: 1,
      Environment: {
        Language: "cpp",
        Tools: "clang",
        AllowStudentCreateFiles: false,
      },
      Data: { Files: {} },
      Tests: { Public: [], Private: [] },
      Grading: { MaxScore: 100 },
    };
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Code",
          lessonFormat: null,
          body: null,
          jsonBody: codingAssignment,
        },
      } as never,
      {
        activeItem: { ...item, type: "Code" } as never,
        linkedAssessment: {
          id: "assessment-1",
          slug: "code-assessment",
          title: "Code assessment",
        },
        initialCodingAssignment: codingAssignment,
      },
    );

    expect(screen.getByTestId("coding-definition-editor")).toHaveAttribute(
      "data-embedded",
      "true",
    );
    expect(
      screen.getByText("Coding assignment", { exact: true }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText("Markdown", { exact: true }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("textbox", { name: "Lesson body" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByTestId("coding-assignment-preview"),
    ).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /preview/i }));
    expect(screen.getByTestId("coding-assignment-preview")).toBeInTheDocument();
    expect(screen.queryByTestId("learner-renderer")).not.toBeInTheDocument();
  });

  it("opens the coding editor before the content-owned assessment exists and refreshes after save", () => {
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Code",
          lessonFormat: null,
          body: null,
          jsonBody: null,
        },
      } as never,
      {
        activeItem: { ...item, type: "Code" } as never,
        linkedAssessment: null,
        initialCodingAssignment: null,
      },
    );

    fireEvent.click(screen.getByRole("button", { name: /preview/i }));
    expect(
      screen.getByText(/save the coding assignment to preview/i),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("learner-renderer")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /editor/i }));
    fireEvent.click(screen.getByTestId("coding-definition-editor"));
    expect(mocks.refresh).toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: /preview/i }));
    expect(screen.getByTestId("coding-assignment-preview")).toBeInTheDocument();
  });

  it("opens the coding editor when the content tree is Code but a legacy draft still says Lesson", () => {
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Lesson",
          lessonFormat: "Markdown",
          body: "Legacy markdown payload",
          jsonBody: null,
        },
      } as never,
      {
        activeItem: { ...item, type: "Code" } as never,
        linkedAssessment: null,
        initialCodingAssignment: null,
      },
    );

    expect(screen.getByTestId("coding-definition-editor")).toBeInTheDocument();
    expect(
      screen.queryByRole("textbox", { name: "Lesson body" }),
    ).not.toBeInTheDocument();
  });

  it("opens the quiz editor when the content tree is Questionnaire but a legacy draft still says Lesson", () => {
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Lesson",
          lessonFormat: "Markdown",
          body: "Legacy markdown payload",
          jsonBody: null,
        },
      } as never,
      {
        activeItem: { ...item, type: "Questionnaire" } as never,
      },
    );

    expect(
      screen.getByRole("button", { name: "Quiz editor" }),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("quiz-preview")).not.toBeInTheDocument();
    expect(
      screen.queryByRole("textbox", { name: "Lesson body" }),
    ).not.toBeInTheDocument();
  });

  it("creates a gradebook assessment when Graded is toggled on for Code content", async () => {
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Code",
          lessonFormat: null,
          body: null,
          jsonBody: null,
        },
      } as never,
      {
        activeItem: { ...item, type: "Code" } as never,
        linkedAssessment: null,
        initialCodingAssignment: null,
      },
    );

    fireEvent.click(screen.getByRole("switch", { name: "Graded" }));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mocks.createAssessment).toHaveBeenCalledWith(
      expect.objectContaining({
        courseId: "course-1",
        title: "Original lesson",
        type: "Assignment",
        contentId: "lesson-1",
        submissionModalities: "Code",
      }),
    );
    expect(mocks.refresh).toHaveBeenCalled();
  });

  it("soft-deletes the linked assessment after confirming Graded off", async () => {
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Code",
          lessonFormat: null,
          body: null,
          jsonBody: null,
        },
      } as never,
      {
        activeItem: { ...item, type: "Code" } as never,
        linkedAssessment: {
          id: "assessment-1",
          slug: "test",
          title: "Test",
        } as never,
        initialCodingAssignment: null,
      },
    );

    fireEvent.click(screen.getByRole("switch", { name: "Graded" }));
    fireEvent.click(screen.getByRole("button", { name: "Remove grading" }));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mocks.deleteAssessment).toHaveBeenCalledWith("course-1", "assessment-1");
  });

  it("restores a recently deleted assessment instead of creating a new one", async () => {
    const assignment = {
      Type: "coding-assignment",
      Version: 1,
      Environment: { Language: "cpp", Tools: "", AllowStudentCreateFiles: false },
      Data: { Files: {} },
      Tests: { Public: [{}], Private: [{}] },
      Grading: { MaxScore: 100 },
    } as never;
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Code",
          lessonFormat: null,
          body: null,
          jsonBody: null,
        },
      } as never,
      {
        activeItem: { ...item, type: "Code" } as never,
        linkedAssessment: {
          id: "assessment-1",
          slug: "test",
          title: "Test",
        } as never,
        initialCodingAssignment: assignment,
      },
    );

    fireEvent.click(screen.getByRole("switch", { name: "Graded" }));
    fireEvent.click(screen.getByRole("button", { name: "Remove grading" }));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    fireEvent.click(screen.getByRole("switch", { name: "Graded" }));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mocks.createAssessment).not.toHaveBeenCalled();
    expect(mocks.restoreAssessment).toHaveBeenCalledWith(
      "course-1",
      "assessment-1",
    );
  });

  it("renders the coding test summary for an auto-graded Code item", () => {
    const assignment = {
      Type: "coding-assignment",
      Version: 1,
      Environment: { Language: "cpp", Tools: "", AllowStudentCreateFiles: false },
      Data: { Files: {} },
      Tests: { Public: [{}], Private: [{}, {}] },
      Grading: { MaxScore: 100 },
    } as never;
    renderWorkspace(
      {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type: "Code",
          lessonFormat: null,
          body: null,
          jsonBody: null,
        },
      } as never,
      {
        activeItem: { ...item, type: "Code" } as never,
        linkedAssessment: {
          id: "assessment-1",
          slug: "test",
          title: "Test",
          reviewMethods: 12,
        } as never,
        initialCodingAssignment: assignment,
      },
    );

    expect(screen.getByText("Coding tests")).toBeInTheDocument();
    expect(screen.getByText(/3 \(1 public\)/)).toBeInTheDocument();
    expect(screen.getByText(/cpp/)).toBeInTheDocument();
  });

  it("replaces the stale authoring URL after publishing a changed slug", async () => {
    mocks.publishDraft.mockResolvedValueOnce({
      success: true,
      data: {
        draft: { ...initialDraft, revision: 2, basePublishedVersion: 4 },
        publishedContent: { slug: "renamed-lesson" },
      },
    });
    renderWorkspace();
    fireEvent.change(screen.getByLabelText("URL slug"), {
      target: { value: "renamed-lesson" },
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Publish changes" }));
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mocks.replace).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-slug/content/renamed-lesson",
    );
  });

  it.each([
    [new Error("Assets are not promoted"), "Assets are not promoted"],
    ["asset failure", "Lesson assets are not ready to publish."],
  ])(
    "blocks publication when assets are unavailable",
    async (failure, message) => {
      mocks.prepareAssets.mockRejectedValueOnce(failure);
      renderWorkspace();

      await act(async () => {
        fireEvent.click(
          screen.getByRole("button", { name: "Publish changes" }),
        );
        await Promise.resolve();
      });

      expect(screen.getByText(message)).toBeInTheDocument();
      expect(mocks.publishDraft).not.toHaveBeenCalled();
    },
  );

  it.each([
    [409, "Conflict"],
    [503, "Offline"],
  ])(
    "maps publication status %s to the editor state",
    async (status, expectedState) => {
      mocks.publishDraft.mockResolvedValueOnce({
        success: false,
        error: `Publish failed ${status}`,
        status,
      });
      renderWorkspace();

      await act(async () => {
        fireEvent.click(
          screen.getByRole("button", { name: "Publish changes" }),
        );
        await Promise.resolve();
      });

      expect(screen.getByText(`Publish failed ${status}`)).toBeInTheDocument();
      expect(screen.getByText(expectedState)).toBeInTheDocument();
    },
  );

  it("does not publish when the dirty draft cannot be saved", async () => {
    mocks.saveDraft.mockResolvedValueOnce({
      success: false,
      error: "Save failed",
      status: 503,
    });
    renderWorkspace();
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Unsaved" },
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Publish changes" }));
      await Promise.resolve();
    });

    expect(mocks.publishDraft).not.toHaveBeenCalled();
  });

  it("supports save and Copilot keyboard shortcuts and unload protection", async () => {
    renderWorkspace();
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Unsaved shortcut lesson" },
    });
    const unload = new Event("beforeunload", { cancelable: true });
    window.dispatchEvent(unload);
    expect(unload.defaultPrevented).toBe(true);

    await act(async () => {
      fireEvent.keyDown(window, { key: "s", ctrlKey: true });
      await Promise.resolve();
    });
    expect(mocks.saveDraft).toHaveBeenCalled();

    await act(async () => {
      fireEvent.keyDown(window, { key: "i", metaKey: true, shiftKey: true });
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(
      screen.getByRole("complementary", { name: "AI authoring copilot" }),
    ).toBeInTheDocument();
  });

  it("loads the latest Copilot conversation even if entitlement lookup fails", async () => {
    mocks.getEntitlement.mockResolvedValueOnce({
      success: false,
      error: "Wallet unavailable",
      status: 503,
    });
    mocks.getConversations.mockResolvedValueOnce({
      success: true,
      data: [
        {
          id: "conversation-existing",
          messages: [
            {
              id: "message-existing",
              role: "assistant",
              content: "Existing answer",
              createdAt: "2026-09-10T12:00:00Z",
            },
          ],
        },
      ],
    });
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(screen.getByText("Existing answer")).toBeInTheDocument();
    expect(screen.getByText("… SC")).toBeInTheDocument();
  });

  it("streams a proposal, displays usage, and discards it", async () => {
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.getRun.mockResolvedValueOnce({ success: true, data: completedRun });
    mocks.discardProposal.mockResolvedValueOnce({
      success: true,
      data: { ...pendingProposal, status: "Discarded" },
    });
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          streamResponse(
            "event: heartbeat",
            "data: not-json",
            'id: 7\ndata: {"delta":"Hello"}',
            'id: 8\ndata: {"delta":" world","errorCode":"AI_CANCEL_REQUESTED"}',
            'data: {"errorCode":"PROVIDER_WARNING"}',
            `data: ${JSON.stringify({ proposal: pendingProposal })}`,
          ),
        ),
    );
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Add a practical example" }),
      );
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(screen.getByText("Hello world")).toBeInTheDocument();
    expect(screen.getByText("PROVIDER_WARNING")).toBeInTheDocument();
    expect(
      screen.getByText(/Max 20 SC · Used 18 tokens · Settled 3 SC/),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("dialog", { name: "Review AI proposal" }),
    ).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /Unified/ }));
    fireEvent.click(screen.getByRole("button", { name: /Side by side/ }));
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Discard" }));
      await Promise.resolve();
    });
    expect(mocks.discardProposal).toHaveBeenCalledWith(
      "course-1",
      "lesson-1",
      "proposal-1",
    );
  });

  it("accepts an insertion proposal at the current editor cursor", async () => {
    const insertion = { ...pendingProposal, kind: "InsertAtCursor" } as const;
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.getRun.mockResolvedValueOnce({ success: true, data: completedRun });
    mocks.applyProposal.mockResolvedValueOnce({
      success: true,
      data: {
        ...initialDraft,
        revision: 2,
        payload: { ...initialDraft.payload, body: "Improved body" },
      },
    });
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          streamResponse(`data: ${JSON.stringify({ proposal: insertion })}`),
        ),
    );
    renderWorkspace();
    fireEvent.select(screen.getByRole("textbox", { name: "Lesson body" }));
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Ask Copilot" }), {
      target: { value: "Insert an example" },
    });

    await act(async () => {
      fireEvent.keyDown(screen.getByRole("textbox", { name: "Ask Copilot" }), {
        key: "Enter",
      });
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /Accept and apply/ }));
      await Promise.resolve();
    });

    expect(mocks.applyProposal).toHaveBeenCalledWith(
      "course-1",
      "lesson-1",
      "proposal-1",
      1,
      4,
    );
    expect(screen.getByRole("textbox", { name: "Lesson body" })).toHaveValue(
      "Improved body",
    );
  });

  it("marks the draft conflicted when a stale AI proposal is applied", async () => {
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.getRun.mockResolvedValueOnce({ success: true, data: completedRun });
    mocks.applyProposal.mockResolvedValueOnce({
      success: false,
      error: "Proposal is stale",
      status: 409,
    });
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          streamResponse(
            `data: ${JSON.stringify({ proposal: pendingProposal })}`,
          ),
        ),
    );
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", {
          name: "Make this clearer and more concise",
        }),
      );
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /Accept and apply/ }));
      await Promise.resolve();
    });

    expect(screen.getByText("Proposal is stale")).toBeInTheDocument();
    expect(screen.getByText("Conflict")).toBeInTheDocument();
  });

  it("reuses an in-flight save for repeated save commands", async () => {
    let finishSave: ((result: unknown) => void) | undefined;
    mocks.saveDraft.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          finishSave = resolve;
        }),
    );
    renderWorkspace();

    await act(async () => {
      fireEvent.keyDown(window, { key: "s", ctrlKey: true });
      await Promise.resolve();
    });
    fireEvent.keyDown(window, { key: "s", ctrlKey: true });
    expect(mocks.saveDraft).toHaveBeenCalledOnce();

    await act(async () => {
      finishSave?.({ success: true, data: { ...initialDraft, revision: 2 } });
      await Promise.resolve();
    });
  });

  it("opens Copilot from the keyboard on a mobile viewport", async () => {
    vi.mocked(window.matchMedia).mockReturnValue({
      matches: false,
    } as MediaQueryList);
    renderWorkspace();

    await act(async () => {
      fireEvent.keyDown(window, { key: "i", ctrlKey: true, shiftKey: true });
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(
      screen.getByRole("button", { name: "Close lesson panel" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("complementary", { name: "AI authoring copilot" }),
    ).toBeInTheDocument();
  });

  it("uses the compact view selector and both editor-only modes", async () => {
    vi.useRealTimers();
    const user = userEvent.setup();
    renderWorkspace();
    const view = screen.getByRole("combobox", { name: "Editor view" });

    await user.click(view);
    await user.click(screen.getByRole("option", { name: "Preview" }));
    expect(screen.getByTestId("learner-renderer")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "editor" }));
    expect(
      screen.getByRole("textbox", { name: "Lesson body" }),
    ).toBeInTheDocument();
  });

  it("switches right-panel tabs and changes lesson visibility", async () => {
    vi.useRealTimers();
    const user = userEvent.setup();
    renderWorkspace();
    fireEvent.click(screen.getAllByRole("button", { name: /Copilot$/ })[1]!);
    fireEvent.click(screen.getByRole("button", { name: /Settings$/ }));

    const visibility = screen.getByRole("combobox", { name: "Lesson access" });
    await user.click(visibility);
    await user.click(screen.getByRole("option", { name: "Private" }));

    expect(visibility).toHaveTextContent("Private");
  });

  it("uses fallback formats and empty preview fields", () => {
    const noFormat = {
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        lessonFormat: null,
        body: null,
        jsonBody: null,
        description: null,
      },
    };
    renderWorkspace(noFormat as never, {
      activeItem: { ...item, status: "published" } as never,
    });

    fireEvent.click(screen.getByRole("button", { name: "preview" }));
    expect(screen.getByTestId("learner-renderer")).toHaveTextContent("");
    expect(screen.getByText("Published")).toBeInTheDocument();
  });

  it.each([
    ["Questionnaire", "QuizPatch"],
    ["Lesson", "LexicalPatch"],
  ])(
    "uses the required Copilot proposal kind for %s content",
    async (type, expectedKind) => {
      const structuredDraft = {
        ...initialDraft,
        payload: {
          ...initialDraft.payload,
          type,
          lessonFormat: type === "Questionnaire" ? null : "Lexical",
          body: null,
          jsonBody: { root: { children: [] } },
        },
      };
      mocks.createRun.mockResolvedValueOnce({
        success: false,
        error: "Captured",
        status: 400,
      });
      renderWorkspace(structuredDraft as never);
      fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));

      await act(async () => {
        fireEvent.click(
          screen.getByRole("button", {
            name: "Create a short knowledge check",
          }),
        );
        await Promise.resolve();
      });

      expect(mocks.createRun).toHaveBeenCalledWith(
        "course-1",
        "lesson-1",
        expect.objectContaining({ proposalKind: expectedKind }),
      );
    },
  );

  it("does not start Copilot for an empty prompt or an unsaved draft", async () => {
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    fireEvent.keyDown(screen.getByRole("textbox", { name: "Ask Copilot" }), {
      key: "Enter",
    });
    expect(mocks.createRun).not.toHaveBeenCalled();

    mocks.saveDraft.mockResolvedValueOnce({
      success: false,
      error: "Save first",
      status: 503,
    });
    fireEvent.change(screen.getByRole("textbox", { name: "Lesson title" }), {
      target: { value: "Dirty lesson" },
    });
    fireEvent.change(screen.getByRole("textbox", { name: "Ask Copilot" }), {
      target: { value: "Improve it" },
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Send to Copilot" }));
      await Promise.resolve();
    });
    expect(mocks.createRun).not.toHaveBeenCalled();
  });

  it("reconnects the Copilot stream with its last event id", async () => {
    const completedWithProposal = {
      ...completedRun,
      proposal: pendingProposal,
    };
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    let runLookup = 0;
    mocks.getRun.mockImplementation(async () => {
      runLookup += 1;
      if (runLookup === 1)
        return {
          success: false,
          error: "Temporary lookup failure",
          status: 503,
        };
      if (runLookup === 2) return { success: true, data: runningRun };
      return { success: true, data: completedWithProposal };
    });
    mocks.getEntitlement.mockResolvedValue({
      success: false,
      error: "Wallet refresh failed",
      status: 503,
    });
    const fetchMock = vi
      .fn()
      .mockImplementation(async () =>
        streamResponse('id: 9\ndata: {"delta":"Reconnected"}'),
      );
    vi.stubGlobal("fetch", fetchMock);
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Add a practical example" }),
      );
      await flushCopilotRetries(4);
    });

    expect(mocks.getRun).toHaveBeenCalledTimes(3);
    expect(
      fetchMock.mock.calls.some(([, init]) =>
        JSON.stringify(init).includes('"Last-Event-ID":"9"'),
      ),
    ).toBe(true);
    expect(
      screen.getByRole("dialog", { name: "Review AI proposal" }),
    ).toBeInTheDocument();
  });

  it.each([
    [{ ok: false, body: null }, "The Copilot stream could not be opened."],
    [{ ok: true, body: null }, "The Copilot stream could not be opened."],
  ])(
    "stops after repeated stream connection failures",
    async (response, message) => {
      mocks.createRun.mockResolvedValueOnce({
        success: true,
        data: runningRun,
      });
      vi.stubGlobal("fetch", vi.fn().mockResolvedValue(response));
      renderWorkspace();
      fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));

      await act(async () => {
        fireEvent.click(
          screen.getByRole("button", { name: "Add a practical example" }),
        );
        await flushCopilotRetries();
      });

      expect(screen.getByText(message)).toBeInTheDocument();
      expect(fetch).toHaveBeenCalledTimes(5);
    },
  );

  it("normalizes a non-Error Copilot stream failure", async () => {
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue("offline"));
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Add a practical example" }),
      );
      await flushCopilotRetries();
    });

    expect(
      screen.getByText("Copilot stopped unexpectedly."),
    ).toBeInTheDocument();
  });

  it.each([
    [undefined, "Copilot generation failed."],
    ["Provider failed", "Provider failed"],
  ])("reports a failed Copilot run", async (errorMessage, expectedMessage) => {
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.getRun.mockResolvedValueOnce({
      success: true,
      data: { ...completedRun, status: "Failed", errorMessage },
    });
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(streamResponse('data: {"delta":"Partial"}')),
    );
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Add a practical example" }),
      );
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(screen.getByText(expectedMessage)).toBeInTheDocument();
  });

  it("restores and clears missing or finished Copilot runs", async () => {
    window.sessionStorage.setItem(
      "authoring-ai-run:course-1:lesson-1",
      "missing-run",
    );
    mocks.getRun.mockResolvedValueOnce({
      success: false,
      error: "Missing",
      status: 404,
    });
    const first = renderWorkspace();
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(
      window.sessionStorage.getItem("authoring-ai-run:course-1:lesson-1"),
    ).toBeNull();
    first.unmount();

    window.sessionStorage.setItem(
      "authoring-ai-run:course-1:lesson-1",
      "finished-run",
    );
    mocks.getRun.mockResolvedValueOnce({
      success: true,
      data: {
        ...completedRun,
        id: "finished-run",
        proposal: { ...pendingProposal, status: "Applied" },
      },
    });
    renderWorkspace();
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(
      window.sessionStorage.getItem("authoring-ai-run:course-1:lesson-1"),
    ).toBeNull();
    expect(
      screen.queryByRole("dialog", { name: "Review AI proposal" }),
    ).not.toBeInTheDocument();
  });

  it("does not restore a second run after the component identity changes", async () => {
    window.sessionStorage.setItem(
      "authoring-ai-run:course-1:lesson-1",
      "run-1",
    );
    mocks.getRun.mockResolvedValueOnce({ success: true, data: completedRun });
    const view = render(
      <LessonAuthoringWorkspace
        courseId="course-1"
        courseSlug="course-slug"
        courseTitle="Course title"
        item={item}
        curriculum={[item]}
        initialDraft={initialDraft as never}
      />,
    );
    await act(async () => {
      await Promise.resolve();
    });
    view.rerender(
      <LessonAuthoringWorkspace
        courseId="course-2"
        courseSlug="course-slug"
        courseTitle="Course title"
        item={item}
        curriculum={[item]}
        initialDraft={initialDraft as never}
      />,
    );
    await act(async () => {
      await Promise.resolve();
    });
    expect(mocks.getRun).toHaveBeenCalledOnce();
  });

  it("keeps a running Copilot active when cancellation fails or is deferred", async () => {
    window.sessionStorage.setItem(
      "authoring-ai-run:course-1:lesson-1",
      runningRun.id,
    );
    mocks.getRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.cancelRun
      .mockResolvedValueOnce({
        success: false,
        error: "Cancellation failed",
        status: 503,
      })
      .mockResolvedValueOnce({
        success: true,
        data: { ...runningRun, status: "Running" },
      });
    vi.stubGlobal(
      "fetch",
      vi.fn(
        (_url: string, init?: RequestInit) =>
          new Promise((_resolve, reject) => {
            init?.signal?.addEventListener("abort", () =>
              reject(new DOMException("Aborted", "AbortError")),
            );
          }),
      ),
    );
    renderWorkspace();
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Stop" }));
      await Promise.resolve();
    });
    expect(screen.getByText("Cancellation failed")).toBeInTheDocument();
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Stop" }));
      await Promise.resolve();
    });
    expect(screen.getByText("Generating proposal…")).toBeInTheDocument();
  });

  it("ignores a stop click until run creation returns", async () => {
    let resolveCreate: ((result: unknown) => void) | undefined;
    mocks.createRun.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveCreate = resolve;
        }),
    );
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Add a practical example" }),
    );
    await act(async () => {
      await Promise.resolve();
    });
    fireEvent.click(screen.getByRole("button", { name: "Stop" }));
    expect(mocks.cancelRun).not.toHaveBeenCalled();

    await act(async () => {
      resolveCreate?.({ success: false, error: "Stopped", status: 400 });
      await Promise.resolve();
    });
  });

  it("keeps the previous balance when cancellation cannot refresh it", async () => {
    window.sessionStorage.setItem(
      "authoring-ai-run:course-1:lesson-1",
      runningRun.id,
    );
    mocks.getRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.cancelRun.mockResolvedValueOnce({
      success: true,
      data: { ...runningRun, status: "Cancelled", errorCode: "AI_CANCELLED" },
    });
    mocks.getEntitlement.mockResolvedValue({
      success: false,
      error: "Wallet unavailable",
      status: 503,
    });
    vi.stubGlobal(
      "fetch",
      vi.fn(
        (_url: string, init?: RequestInit) =>
          new Promise((_resolve, reject) => {
            init?.signal?.addEventListener("abort", () =>
              reject(new DOMException("Aborted", "AbortError")),
            );
          }),
      ),
    );
    renderWorkspace();
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Stop" }));
      await Promise.resolve();
    });

    expect(
      screen.queryByRole("button", { name: "Stop" }),
    ).not.toBeInTheDocument();
  });

  it("keeps the diff open when proposal application fails without a conflict", async () => {
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.getRun.mockResolvedValueOnce({ success: true, data: completedRun });
    mocks.applyProposal.mockResolvedValueOnce({
      success: false,
      error: "Apply unavailable",
      status: 503,
    });
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          streamResponse(
            `data: ${JSON.stringify({ proposal: pendingProposal })}`,
          ),
        ),
    );
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Add a practical example" }),
      );
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /Accept and apply/ }));
      await Promise.resolve();
    });
    expect(screen.getByText("Apply unavailable")).toBeInTheDocument();
    expect(screen.queryByText("Conflict")).not.toBeInTheDocument();
  });

  it("closes the diff even when discarding the proposal fails", async () => {
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.getRun.mockResolvedValueOnce({ success: true, data: completedRun });
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          streamResponse(
            `data: ${JSON.stringify({ proposal: pendingProposal })}`,
          ),
        ),
    );
    renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Add a practical example" }),
      );
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Discard" }));
      await Promise.resolve();
    });

    expect(
      screen.queryByRole("dialog", { name: "Review AI proposal" }),
    ).not.toBeInTheDocument();
  });

  it("renders a null structured body safely in preview", () => {
    renderWorkspace({
      ...initialDraft,
      payload: {
        ...initialDraft.payload,
        lessonFormat: "Lexical",
        body: null,
        jsonBody: null,
      },
    } as never);

    fireEvent.click(screen.getByRole("button", { name: "preview" }));
    expect(screen.getByTestId("learner-renderer")).toHaveTextContent("null");
  });

  it.each([
    [new Error("Restored stream failed"), "Restored stream failed"],
    ["offline", "Copilot stopped unexpectedly."],
  ])(
    "reports restoration stream failures",
    async (failure, expectedMessage) => {
      window.sessionStorage.setItem(
        "authoring-ai-run:course-1:lesson-1",
        runningRun.id,
      );
      mocks.getRun.mockResolvedValueOnce({ success: true, data: runningRun });
      vi.stubGlobal("fetch", vi.fn().mockRejectedValue(failure));
      renderWorkspace();

      await act(async () => {
        await flushCopilotRetries();
      });

      expect(screen.getByText(expectedMessage)).toBeInTheDocument();
    },
  );

  it("shows a pending cancellation and protects the stop action", async () => {
    const cancellingRun = { ...runningRun, errorCode: "AI_CANCEL_REQUESTED" };
    window.sessionStorage.setItem(
      "authoring-ai-run:course-1:lesson-1",
      cancellingRun.id,
    );
    mocks.getRun.mockResolvedValueOnce({ success: true, data: cancellingRun });
    vi.stubGlobal(
      "fetch",
      vi.fn(
        (_url: string, init?: RequestInit) =>
          new Promise((_resolve, reject) => {
            init?.signal?.addEventListener("abort", () =>
              reject(new DOMException("Aborted", "AbortError")),
            );
          }),
      ),
    );
    renderWorkspace();
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(screen.getByText("Stopping Copilot…")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Stop" })).toBeDisabled();
  });

  it("selects insertion mode and preserves Shift+Enter in the prompt", async () => {
    vi.useRealTimers();
    const user = userEvent.setup();
    mocks.createRun.mockResolvedValueOnce({
      success: false,
      error: "Captured",
      status: 400,
    });
    renderWorkspace();
    await user.click(screen.getByRole("button", { name: "Open Copilot" }));
    const mode = screen.getByRole("combobox", { name: "Proposal application" });
    await user.click(mode);
    await user.click(screen.getByRole("option", { name: "Insert at cursor" }));
    const prompt = screen.getByRole("textbox", { name: "Ask Copilot" });
    await user.type(prompt, "Add a note");
    fireEvent.keyDown(prompt, { key: "Enter", shiftKey: true });
    expect(mocks.createRun).not.toHaveBeenCalled();

    fireEvent.keyDown(prompt, { key: "Enter" });
    await act(async () => {
      await Promise.resolve();
    });
    expect(mocks.createRun).toHaveBeenCalledWith(
      "course-1",
      "lesson-1",
      expect.objectContaining({ proposalKind: "InsertAtCursor" }),
    );
  });

  it("configures a light HTML diff", async () => {
    mocks.theme = "light";
    mocks.createRun.mockResolvedValueOnce({ success: true, data: runningRun });
    mocks.getRun.mockResolvedValueOnce({ success: true, data: completedRun });
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          streamResponse(
            `data: ${JSON.stringify({ proposal: pendingProposal })}`,
          ),
        ),
    );
    renderWorkspace({
      ...initialDraft,
      payload: { ...initialDraft.payload, lessonFormat: "Html" },
    } as never);
    fireEvent.click(screen.getByRole("button", { name: "Open Copilot" }));
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: "Add a practical example" }),
      );
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mocks.diffEditorProps).toEqual(
      expect.objectContaining({ language: "html", theme: "vs-light" }),
    );
  });
});
