import "@testing-library/jest-dom/vitest";
import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ContentItemEditor } from "./content-item-editor";
import {
  createAssessment,
  deleteAssessment,
  restoreAssessment,
  saveQuizAssessmentDraft,
  updateAssessment,
  updateContent,
} from "@/lib/learning/actions";
import type { Assessment } from "@/lib/learning/queries/assessments";
import type { ContentItemDetail } from "@/lib/learning/types";

const routerMocks = vi.hoisted(() => ({
  back: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
  replace: vi.fn(),
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
  usePathname: () => "/workspace/learning",
  useRouter: () => routerMocks,
}));

vi.mock("@/lib/learning/actions", () => ({
  updateContent: vi.fn(),
  createAssessment: vi.fn(),
  deleteAssessment: vi.fn(),
  restoreAssessment: vi.fn(),
  saveQuizAssessmentDraft: vi.fn(),
  updateAssessment: vi.fn(),
}));

const putCodingDefinitionMock = vi.hoisted(() => ({
  putCodingDefinition: vi.fn(),
}));

vi.mock("@/lib/emception/put-coding-definition", () => ({
  putCodingDefinition: putCodingDefinitionMock.putCodingDefinition,
}));

vi.mock("@game-guild/lexical-surface", () => ({
  LexicalSurface: ({
    accessibleLabel,
    onChange,
  }: {
    accessibleLabel?: string;
    onChange?: (value: Record<string, unknown>) => void;
  }) => (
    <button
      type="button"
      aria-label={accessibleLabel ?? "Body"}
      onClick={() =>
        onChange?.({
          root: {
            type: "root",
            children: [
              {
                type: "paragraph",
                children: [{ type: "text", text: "Changed" }],
              },
            ],
          },
        })
      }
    >
      Change rich text
    </button>
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

const item = {
  id: "content-1",
  version: 1,
  parentId: "module-1",
  order: 1,
  type: "Questionnaire",
  title: "Intro quiz",
  slug: "intro-quiz",
  description: "First knowledge check.",
  status: "published",
  visibility: "Public",
  duration: 20,
  estimatedMinutesSource: "Manual",
  metadata: {},
  gradingConfig: null,
  content: null,
  jsonBody: { schemaVersion: 1, order: [], blocks: {} },
  settings: { isRequired: true },
  lessonFormat: null,
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
} satisfies ContentItemDetail;

const quizItemWithBlock = {
  ...item,
  id: "content-2",
  jsonBody: {
    schemaVersion: 1,
    order: [["1", "quiz"]],
    blocks: {
      "1": {
        type: "TRUE_FALSE",
        stem: "Stored question",
        correctAnswer: true,
        settings: {
          allowRetry: true,
          showFeedback: true,
          showCorrectAnswer: true,
        },
      },
    },
  },
} satisfies ContentItemDetail;

const lessonItemMarkdownEmpty = {
  id: "lesson-1",
  version: 1,
  parentId: "module-1",
  order: 2,
  type: "Lesson",
  title: "Intro lesson",
  slug: "intro-lesson",
  description: "Markdown-format lesson.",
  status: "published",
  visibility: "Public",
  duration: 15,
  estimatedMinutesSource: "Manual",
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
  version: 1,
  parentId: "module-1",
  order: 3,
  type: "Lesson",
  title: "Markdown lesson",
  slug: "markdown-lesson",
  description: "Has a Markdown body.",
  status: "published",
  visibility: "Public",
  duration: 15,
  estimatedMinutesSource: "Manual",
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
  version: 1,
  parentId: "module-1",
  order: 4,
  type: "Lesson",
  title: "Lexical lesson",
  slug: "lexical-lesson",
  description: "Has a Lexical body.",
  status: "published",
  visibility: "Public",
  duration: 15,
  estimatedMinutesSource: "Manual",
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

const gradedQuizItem = {
  ...item,
  description: null,
  jsonBody: {
    schemaVersion: 1,
    order: [],
    blocks: {},
    grading: {
      enabled: true,
      schemaVersion: 1,
      score: { maxScore: 10 },
      attempts: {},
      feedback: {},
      presentation: { mode: "single-step" },
      items: {},
    },
  },
} satisfies ContentItemDetail;

const lessonItemHtml = {
  ...lessonItemMarkdownBody,
  id: "lesson-html",
  title: "HTML lesson",
  slug: "html-lesson",
  description: "Has an HTML body.",
  content: "<h1>Existing HTML</h1>",
  lessonFormat: "Html",
} satisfies ContentItemDetail;

const lessonItemVideo = {
  ...lessonItemMarkdownBody,
  id: "lesson-video",
  title: "Video lesson",
  slug: "video-lesson",
  description: "Has a video URL.",
  content: "https://video.example.test/original.mp4",
  lessonFormat: "Video",
} satisfies ContentItemDetail;

const lessonItemExternalLink = {
  ...lessonItemMarkdownBody,
  id: "lesson-link",
  title: "External resource",
  slug: "external-resource",
  description: "Links to a resource.",
  content: "https://example.test/original",
  lessonFormat: "ExternalLink",
} satisfies ContentItemDetail;

// ── Task 10: reading-time estimation (auto hint / manual pin) ──

// 400 words at 200 wpm → ~2 min auto estimate.
const fourHundredWords = Array.from({ length: 400 }, (_, i) => `word${i}`).join(
  " ",
);

const lessonItemMarkdownAutoHint = {
  ...lessonItemMarkdownBody,
  id: "lesson-auto",
  title: "Auto estimate lesson",
  slug: "auto-estimate-lesson",
  content: fourHundredWords,
  duration: null,
  estimatedMinutesSource: null,
} satisfies ContentItemDetail;

const lessonItemManualDuration = {
  ...lessonItemMarkdownBody,
  id: "lesson-manual",
  title: "Manual duration lesson",
  slug: "manual-duration-lesson",
  duration: 20,
  estimatedMinutesSource: "Manual",
} satisfies ContentItemDetail;

// ── Assignment / Project: coding-assignment bridge ──

const assignmentItem = {
  id: "content-asn",
  version: 1,
  parentId: "module-1",
  order: 5,
  type: "Assignment",
  title: "Hello world coding task",
  slug: "hello-world-coding-task",
  description: "Echo stdin to stdout.",
  status: "published",
  visibility: "Public",
  duration: 60,
  estimatedMinutesSource: "Manual",
  metadata: {},
  gradingConfig: null,
  content: null,
  jsonBody: null,
  settings: { isRequired: true },
  lessonFormat: null,
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
} satisfies ContentItemDetail;

function assessmentFixture(overrides: Partial<Assessment> = {}): Assessment {
  return {
    id: "asmnt-1",
    slug: "asmnt-1",
    courseId: "course-1",
    contentId: "content-1",
    assessmentGroupId: null,
    assessmentGroupName: null,
    assessmentGroupWeightPercent: null,
    assessmentGroupOrder: null,
    title: "Assessment",
    description: null,
    type: "Quiz",
    maxScore: 1,
    passingScore: 0,
    timeLimitMinutes: null,
    maxAttempts: 1,
    isRequired: true,
    order: 0,
    availableFrom: null,
    availableUntil: null,
    presentationMode: "Continuous",
    dueAt: null,
    allowLateSubmissions: false,
    lateSubmissionDeadline: null,
    isAvailable: true,
    reviewMethods: 8 as Assessment["reviewMethods"],
    groupSetId: null,
    publishedDefinitionRevisionId: null,
    reviewConfigurationCanonicalJson: null,
    attemptContributionMode: null,
    contentCompletionMode: "on-release-and-pass",
    resultReleaseMode: "manual",
    resultReleaseScheduledFor: null,
    version: 1,
    ...overrides,
  };
}

describe("ContentItemEditor", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(saveQuizAssessmentDraft).mockResolvedValue({
      success: true,
      data: {
        contentId: "content-1",
        contentVersion: 2,
        assessmentId: null,
        assessmentVersion: null,
      },
    });
    vi.mocked(createAssessment).mockResolvedValue({
      success: true,
      data: { id: "new-asmnt" },
    });
    vi.mocked(deleteAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(restoreAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(updateAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
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

    expect(await screen.findByText("Stored question")).toBeInTheDocument();
    expect(screen.getByText("True / false")).toBeInTheDocument();
  });

  it("creates an empty quiz document when legacy quiz JSON is absent", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={{ ...item, jsonBody: null }}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));
    await waitFor(() => {
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({ document: {} }),
      );
    });
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
    expect(saveQuizAssessmentDraft).not.toHaveBeenCalled();
  });

  it("normalizes legacy items whose title and slug are absent", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={
          {
            ...lessonItemMarkdownEmpty,
            title: undefined,
            slug: undefined,
          } as unknown as ContentItemDetail
        }
        courseTitle="Advanced Game AI"
      />,
    );

    expect(screen.getByLabelText(/^title$/i)).toHaveValue("");
    expect(screen.getByLabelText(/url slug/i)).toHaveValue("");
    await user.type(screen.getByLabelText(/^title$/i), "Recovered lesson");
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          title: "Recovered lesson",
          slug: "recovered-lesson",
        }),
      );
    });
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
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({
          courseId: "course-1",
          contentId: "content-1",
          expectedContentVersion: 1,
          expectedAssessmentVersion: null,
          title: "Updated quiz",
          slug: "updated-quiz",
          description: "Updated description.",
          document: item.jsonBody,
          visibility: "Public",
          isRequired: true,
          estimatedMinutes: 35,
          estimatedMinutesSource: "Manual",
        }),
      );
    });
    expect(routerMocks.replace).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-1/content/updated-quiz",
    );
    expect(routerMocks.refresh).not.toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
  });

  it("slug follows the title while untouched, then detaches and normalizes once edited directly", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    const titleInput = screen.getByLabelText(/^title$/i);
    const slugInput = screen.getByLabelText(/url slug/i);
    expect(slugInput).toHaveValue("intro-quiz");

    await user.clear(titleInput);
    await user.type(titleInput, "Advanced Quiz!!");
    expect(slugInput).toHaveValue("advanced-quiz");

    await user.clear(slugInput);
    await user.type(slugInput, "My Custom SLUG!");
    expect(slugInput).toHaveValue("my-custom-slug");

    await user.clear(titleInput);
    await user.type(titleInput, "Another Title");
    expect(slugInput).toHaveValue("my-custom-slug");

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({
          title: "Another Title",
          slug: "my-custom-slug",
        }),
      );
    });
  });

  it("regenerates the slug from the title even when the stored slug does not mirror it", async () => {
    const user = userEvent.setup();
    const backfilled = {
      ...item,
      slug: "0d5ee1a2-9c4b-4d0e-8f7a-3b2c1d0e5f6a",
    };
    render(
      <ContentItemEditor
        courseId="course-1"
        item={backfilled}
        courseTitle="Advanced Game AI"
      />,
    );

    const slugInput = screen.getByLabelText(/url slug/i);
    expect(slugInput).toHaveValue("0d5ee1a2-9c4b-4d0e-8f7a-3b2c1d0e5f6a");

    await user.clear(screen.getByLabelText(/^title$/i));
    await user.type(screen.getByLabelText(/^title$/i), "New Title");
    expect(slugInput).toHaveValue("new-title");

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({
          title: "New Title",
          slug: "new-title",
        }),
      );
    });
    expect(routerMocks.replace).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-1/content/new-title",
    );
  });

  it("replaces the URL with the edited slug after saving", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.clear(screen.getByLabelText(/url slug/i));
    await user.type(screen.getByLabelText(/url slug/i), "renamed-slug");
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(routerMocks.replace).toHaveBeenCalledWith(
        "/workspace/learning/courses/course-1/content/renamed-slug",
      );
    });
    expect(routerMocks.refresh).not.toHaveBeenCalled();
  });

  it("keeps a trailing dash while typing spaces and strips it on blur", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    const slugInput = screen.getByLabelText(/url slug/i);

    await user.clear(slugInput);
    await user.type(slugInput, " ");
    expect(slugInput).toHaveValue("");

    await user.clear(slugInput);
    await user.type(slugInput, "content ");
    expect(slugInput).toHaveValue("content-");

    await user.type(slugInput, " ");
    expect(slugInput).toHaveValue("content-");

    fireEvent.blur(slugInput);
    expect(slugInput).toHaveValue("content");
  });

  it("derives the slug from the title on save when the slug field is cleared", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.clear(screen.getByLabelText(/url slug/i));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({
          title: "Intro quiz",
          slug: "intro-quiz",
        }),
      );
    });
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
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({
          courseId: "course-1",
          contentId: "content-1",
          title: "Intro quiz",
        }),
      );
    });

    const request = vi.mocked(saveQuizAssessmentDraft).mock.calls[0]![0];
    expect(request.document.grading).toEqual({
      schemaVersion: 2,
      items: {},
    });
    expect(request.reviewMethods).toBe(12);
    expect(createAssessment).not.toHaveBeenCalled();
    expect(updateAssessment).not.toHaveBeenCalled();
  });

  it("keeps one linked assessment while grading is enabled and removes it only when grading is disabled", async () => {
    const user = userEvent.setup();
    vi.mocked(saveQuizAssessmentDraft)
      .mockResolvedValueOnce({
        success: true,
        data: {
          contentId: "content-1",
          contentVersion: 2,
          assessmentId: "asmnt-quiz",
          assessmentVersion: 2,
        },
      })
      .mockResolvedValueOnce({
        success: true,
        data: {
          contentId: "content-1",
          contentVersion: 3,
          assessmentId: null,
          assessmentVersion: null,
        },
      });
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({ id: "asmnt-quiz" })}
      />,
    );

    await user.click(screen.getByRole("switch", { name: /grading/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({ expectedAssessmentVersion: 1 }),
      );
    });
    expect(
      vi.mocked(saveQuizAssessmentDraft).mock.calls[0]![0].document,
    ).toHaveProperty("grading");

    await user.click(screen.getByRole("switch", { name: /grading/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(saveQuizAssessmentDraft).toHaveBeenCalledTimes(2);
    });
    expect(
      vi.mocked(saveQuizAssessmentDraft).mock.calls[1]![0].document,
    ).not.toHaveProperty("grading");
    expect(deleteAssessment).not.toHaveBeenCalled();
  });

  it("leaves result placement to assessment groups", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("switch", { name: /grading/i }));
    expect(
      screen.queryByRole("combobox", { name: /result destination/i }),
    ).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => expect(saveQuizAssessmentDraft).toHaveBeenCalledOnce());
    expect(createAssessment).not.toHaveBeenCalled();
    expect(deleteAssessment).not.toHaveBeenCalled();

    const jsonBody = vi.mocked(saveQuizAssessmentDraft).mock.calls[0]![0]
      .document as {
      grading?: Record<string, unknown>;
    };
    expect(jsonBody.grading).not.toHaveProperty("outcome");
  });

  it("shows update errors and routes cancel back to the course content deterministically", async () => {
    const user = userEvent.setup();
    vi.mocked(saveQuizAssessmentDraft).mockResolvedValueOnce({
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

  it("infers Lexical for legacy structured lessons and tolerates missing optional metadata", async () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={
          {
            ...lessonItemLexical,
            lessonFormat: null,
            description: null,
            settings: null,
            updatedAt: "",
          } as ContentItemDetail
        }
        courseTitle="Advanced Game AI"
      />,
    );

    expect(screen.getByLabelText(/lesson format/i)).toHaveValue(
      "Rich text (Lexical)",
    );
    expect(screen.getByRole("switch", { name: /required/i })).toBeChecked();
    expect(screen.queryByTestId("last-saved-at")).not.toBeInTheDocument();
    expect(
      await screen.findByRole("button", { name: "Body" }),
    ).toBeInTheDocument();
  });

  it("updates publication controls and resets a manual duration to Auto", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemManualDuration}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("combobox", { name: /visibility/i }));
    await user.click(screen.getByRole("option", { name: "Private" }));
    await user.click(screen.getByRole("switch", { name: /required/i }));
    await user.click(screen.getByRole("button", { name: "Auto" }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          visibility: "Private",
          isRequired: false,
          estimatedMinutes: null,
          estimatedMinutesSource: "Auto",
        }),
      );
    });
  });

  it("reports a failed quiz assessment creation after saving the content", async () => {
    const user = userEvent.setup();
    vi.mocked(saveQuizAssessmentDraft).mockResolvedValueOnce({
      success: false,
      error: "Assessment creation failed",
    });
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("switch", { name: /grading/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    expect(
      await screen.findByText(/assessment creation failed/i),
    ).toBeInTheDocument();
    expect(createAssessment).not.toHaveBeenCalled();
  });

  it("reports a failed linked quiz assessment update and maps single-step settings", async () => {
    const user = userEvent.setup();
    vi.mocked(saveQuizAssessmentDraft).mockResolvedValueOnce({
      success: false,
      error: "Assessment update failed",
    });
    render(
      <ContentItemEditor
        courseId="course-1"
        item={gradedQuizItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-quiz",
          contentId: gradedQuizItem.id,
          presentationMode: "SingleStep",
          maxAttempts: 1,
        })}
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(saveQuizAssessmentDraft).toHaveBeenCalledWith(
        expect.objectContaining({
          expectedAssessmentVersion: 1,
          description: undefined,
          timeLimitMinutes: null,
          maxAttempts: 1,
          presentationMode: "SingleStep",
        }),
      );
    });
    expect(
      await screen.findByText(/assessment update failed/i),
    ).toBeInTheDocument();
  });

  it("reports a failed linked quiz assessment removal", async () => {
    const user = userEvent.setup();
    vi.mocked(saveQuizAssessmentDraft).mockResolvedValueOnce({
      success: false,
      error: "Assessment removal failed",
    });
    render(
      <ContentItemEditor
        courseId="course-1"
        item={item}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-quiz",
          contentId: item.id,
        })}
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    expect(
      await screen.findByText(/assessment removal failed/i),
    ).toBeInTheDocument();
  });

  it("fails closed for an unknown lesson format without persisting the body", async () => {
    const user = userEvent.setup();
    const unknownFormatItem = {
      ...lessonItemMarkdownBody,
      lessonFormat: "UnknownFormat",
    } as unknown as ContentItemDetail;

    render(
      <ContentItemEditor
        courseId="course-1"
        item={unknownFormatItem}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("button", { name: /preview/i }));
    expect(
      screen.getByText("This lesson has no published content."),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /save changes/i }));
    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({ body: undefined, jsonBody: undefined }),
      );
    });
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
        slug: "markdown-lesson",
        description: "Has a Markdown body.",
        body: "# existing markdown",
        jsonBody: undefined,
        visibility: "Public",
        isRequired: true,
        estimatedMinutes: 15,
        estimatedMinutesSource: "Manual",
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
        slug: "lexical-lesson",
        description: "Has a Lexical body.",
        body: undefined,
        jsonBody: lessonItemLexical.jsonBody,
        visibility: "Public",
        isRequired: true,
        estimatedMinutes: 15,
        estimatedMinutesSource: "Manual",
        lessonFormat: "Lexical",
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
  });

  it("persists rich-text changes emitted by the Lexical editor", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemLexical}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(await screen.findByRole("button", { name: "Body" }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          contentId: "lesson-3",
          body: undefined,
          lessonFormat: "Lexical",
          jsonBody: {
            root: {
              type: "root",
              children: [
                {
                  type: "paragraph",
                  children: [{ type: "text", text: "Changed" }],
                },
              ],
            },
          },
        }),
      );
    });
  });

  it("saves an explicitly empty Lexical lesson without fabricating document state", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={{ ...lessonItemLexical, jsonBody: null }}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));
    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          body: undefined,
          jsonBody: undefined,
          lessonFormat: "Lexical",
        }),
      );
    });
  });

  it("edits, previews, and saves an HTML lesson body", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemHtml}
        courseTitle="Advanced Game AI"
      />,
    );

    fireEvent.change(screen.getByLabelText("Body"), {
      target: { value: "<h1>Updated HTML</h1>" },
    });
    await user.click(screen.getByRole("button", { name: /preview/i }));
    expect(screen.getByTitle("HTML lesson")).toHaveAttribute(
      "srcdoc",
      "<h1>Updated HTML</h1>",
    );
    await user.click(screen.getByRole("button", { name: /^edit$/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          contentId: "lesson-html",
          body: "<h1>Updated HTML</h1>",
          lessonFormat: "Html",
        }),
      );
    });
  });

  it("edits, previews, and saves a video lesson URL", async () => {
    const user = userEvent.setup();
    const { container } = render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemVideo}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.clear(screen.getByLabelText("Video URL"));
    await user.type(
      screen.getByLabelText("Video URL"),
      "https://video.example.test/updated.mp4",
    );
    await user.click(screen.getByRole("button", { name: /preview/i }));
    await waitFor(() => {
      expect(container.querySelector("video")).toHaveAttribute(
        "src",
        "https://video.example.test/updated.mp4",
      );
    });
    await user.click(screen.getByRole("button", { name: /^edit$/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          contentId: "lesson-video",
          body: "https://video.example.test/updated.mp4",
          lessonFormat: "Video",
        }),
      );
    });
  });

  it("edits, previews, and saves a safe external lesson link", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemExternalLink}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.clear(screen.getByLabelText("External link URL"));
    await user.type(
      screen.getByLabelText("External link URL"),
      "https://example.test/updated",
    );
    await user.click(screen.getByRole("button", { name: /preview/i }));
    expect(
      screen.getByRole("link", { name: /open lesson resource/i }),
    ).toHaveAttribute("href", "https://example.test/updated");
    await user.click(screen.getByRole("button", { name: /^edit$/i }));
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          contentId: "lesson-link",
          body: "https://example.test/updated",
          lessonFormat: "ExternalLink",
        }),
      );
    });
  });

  it("shows the markdown body and preview side by side without a toggle", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownBody}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(
      screen.queryByRole("button", { name: /preview/i }),
    ).not.toBeInTheDocument();
    expect(screen.getByLabelText("Body")).toBeInTheDocument();
    expect(screen.getByTestId("lesson-preview")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { level: 1, name: "existing markdown" }),
    ).toBeInTheDocument();
  });

  it("shows a Preview toggle on RevealJs lessons that swaps the body editor for the learner renderer", async () => {
    const user = userEvent.setup();
    const revealItem = {
      ...lessonItemMarkdownBody,
      id: "lesson-reveal",
      lessonFormat: "RevealJs" as const,
      content: "slide one",
    } satisfies ContentItemDetail;

    render(
      <ContentItemEditor
        courseId="course-1"
        item={revealItem}
        courseTitle="Advanced Game AI"
      />,
    );

    const previewButton = screen.getByRole("button", { name: /preview/i });
    expect(screen.queryByTestId("lesson-preview")).not.toBeInTheDocument();

    await user.click(previewButton);

    expect(screen.getByRole("button", { name: /^edit$/i })).toBeInTheDocument();
    expect(screen.getByTestId("lesson-preview")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /^edit$/i }));
    expect(screen.queryByTestId("lesson-preview")).not.toBeInTheDocument();
  });

  it("autosaves debounced edits without a manual save and updates the last-saved time", async () => {
    vi.useFakeTimers();
    try {
      render(
        <ContentItemEditor
          courseId="course-1"
          item={lessonItemMarkdownBody}
          courseTitle="Advanced Game AI"
        />,
      );

      expect(updateContent).not.toHaveBeenCalled();
      expect(screen.getByTestId("last-saved-at")).toHaveTextContent(
        /last saved \d{1,2}:\d{2}:\d{2}/i,
      );

      fireEvent.change(screen.getByLabelText("Body"), {
        target: { value: "# autosaved markdown" },
      });

      await act(async () => {
        await vi.advanceTimersByTimeAsync(3000);
      });

      expect(updateContent).toHaveBeenCalledTimes(1);
      expect(screen.getByTestId("last-saved-at")).toHaveTextContent(
        /last saved \d{1,2}:\d{2}:\d{2}/i,
      );
      expect(routerMocks.refresh).not.toHaveBeenCalled();
      expect(routerMocks.replace).not.toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });

  it("queues the newest edit while a slower autosave is still in flight", async () => {
    vi.useFakeTimers();
    let resolveFirstSave!: (value: { success: true; data: null }) => void;
    vi.mocked(updateContent)
      .mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveFirstSave = resolve;
          }),
      )
      .mockResolvedValue({ success: true, data: null });

    try {
      render(
        <ContentItemEditor
          courseId="course-1"
          item={lessonItemMarkdownBody}
          courseTitle="Advanced Game AI"
        />,
      );

      fireEvent.change(screen.getByLabelText("Body"), {
        target: { value: "# first edit" },
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(3000);
      });
      expect(updateContent).toHaveBeenCalledTimes(1);

      fireEvent.change(screen.getByLabelText("Body"), {
        target: { value: "# newest edit" },
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(3000);
      });
      expect(updateContent).toHaveBeenCalledTimes(1);

      await act(async () => {
        resolveFirstSave({ success: true, data: null });
        await Promise.resolve();
        await Promise.resolve();
      });

      expect(updateContent).toHaveBeenCalledTimes(2);
      expect(updateContent).toHaveBeenLastCalledWith(
        expect.objectContaining({ body: "# newest edit" }),
      );
    } finally {
      vi.useRealTimers();
    }
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

  it("enables Configure Coding Tests when a linked assessment uses AutomatedReview", async () => {
    const user = userEvent.setup();

    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-1",
          slug: "asmnt-1",
          contentId: codeItem.id,
          reviewMethods: 4 as Assessment["reviewMethods"],
        })}
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

  it("shows every coding-test kind and edits through the stable assessment slug", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-1",
          slug: "sum-two-numbers",
          contentId: codeItem.id,
          reviewMethods: 4 as Assessment["reviewMethods"],
        })}
        initialCodingDefinition={{
          kind: "coding",
          language: "cpp",
          workspaceConfig: { id: "x" },
          testPlan: {
            cases: [
              { kind: "stdio" },
              { kind: "stdio-file" },
              { kind: "doctest" },
              { kind: "clang-query" },
              { kind: "custom" },
            ],
          },
          maxScore: 100,
          passingScore: 60,
        }}
      />,
    );

    const edit = screen.getByRole("button", { name: /edit coding tests/i });
    expect(screen.getByText(/language: cpp/i)).toBeInTheDocument();
    expect(screen.getByText(/test cases: 5/i)).toBeInTheDocument();
    expect(
      screen.getByText(/types: 2 stdin\/stdout · 2 functional/i),
    ).toBeInTheDocument();

    await user.click(edit);
    expect(routerMocks.push).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-1/assessments/sum-two-numbers/coding-definition",
    );
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
    vi.mocked(saveQuizAssessmentDraft).mockResolvedValue({
      success: true,
      data: {
        contentId: "content-1",
        contentVersion: 2,
        assessmentId: null,
        assessmentVersion: null,
      },
    });
    vi.mocked(createAssessment).mockResolvedValue({
      success: true,
      data: { id: "new-asmnt" },
    });
    vi.mocked(deleteAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(restoreAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
  });

  it("renders the Graded toggle for Code content without an automated-review panel when unlinked", () => {
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

  it("renders the Graded switch and coding-tests panel for a linked AutomatedReview assessment", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-1",
          contentId: codeItem.id,
          reviewMethods: 4 as Assessment["reviewMethods"],
        })}
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

  it("keeps the generic Graded toggle outside quizzes", () => {
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
      screen.queryByRole("switch", { name: /^graded$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("switch", { name: /^grading$/i }),
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
          reviewMethods: 12,
        }),
      );
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
  });

  it("can remove an assessment immediately after creating it", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
      />,
    );

    const gradedSwitch = screen.getByRole("switch", { name: /^graded$/i });
    await user.click(gradedSwitch);
    await waitFor(() => {
      expect(createAssessment).toHaveBeenCalledOnce();
      expect(gradedSwitch).not.toBeDisabled();
      expect(gradedSwitch).toBeChecked();
    });

    await user.click(gradedSwitch);
    const dialog = await screen.findByRole("alertdialog");
    await user.click(
      within(dialog).getByRole("button", { name: /remove grading/i }),
    );

    await waitFor(() => {
      expect(deleteAssessment).toHaveBeenCalledWith("course-1", "new-asmnt");
    });
  });

  it("enables coding-test navigation with the assessment returned by creation", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("switch", { name: /^graded$/i }));
    const configure = await screen.findByRole("button", {
      name: /configure coding tests/i,
    });
    await waitFor(() => expect(configure).not.toBeDisabled());
    await user.click(configure);

    expect(routerMocks.push).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-1/assessments/new-asmnt/coding-definition",
    );
  });

  it("shows assessment creation errors and keeps non-code projects unlinked", async () => {
    const user = userEvent.setup();
    vi.mocked(createAssessment).mockResolvedValueOnce({
      success: false,
      error: "Could not create gradebook item",
    });
    render(
      <ContentItemEditor
        courseId="course-1"
        item={projectItem}
        courseTitle="Advanced Game AI"
      />,
    );

    const gradedSwitch = screen.getByRole("switch", { name: /^graded$/i });
    await user.click(gradedSwitch);

    await waitFor(() => {
      expect(createAssessment).toHaveBeenCalledWith(
        expect.objectContaining({
          type: "Project",
          submissionModalities: undefined,
          reviewMethods: 8,
        }),
      );
      expect(gradedSwitch).not.toBeChecked();
    });
    expect(
      await screen.findByText("Could not create gradebook item"),
    ).toBeInTheDocument();
  });

  it("restores a recently soft-deleted assessment when toggled back ON", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={codeItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-existing",
          contentId: codeItem.id,
        })}
      />,
    );

    // Toggle OFF (opens confirm) → confirm → deleteAssessment.
    await user.click(screen.getByRole("switch", { name: /^graded$/i }));
    const dialog = await screen.findByRole("alertdialog");
    await user.click(
      within(dialog).getByRole("button", { name: /remove grading/i }),
    );

    await waitFor(() => {
      expect(deleteAssessment).toHaveBeenCalledWith(
        "course-1",
        "asmnt-existing",
      );
    });

    const gradedSwitch = screen.getByRole("switch", { name: /^graded$/i });
    await waitFor(() => {
      expect(gradedSwitch).not.toBeDisabled();
      expect(gradedSwitch).not.toBeChecked();
    });

    // Toggle ON again → restore (NOT create).
    await user.click(gradedSwitch);

    await waitFor(() => {
      expect(restoreAssessment).toHaveBeenCalledWith(
        "course-1",
        "asmnt-existing",
      );
    });
    expect(createAssessment).not.toHaveBeenCalled();
  });

  it("shows restore failures and leaves grading disabled", async () => {
    const user = userEvent.setup();
    vi.mocked(restoreAssessment).mockResolvedValueOnce({
      success: false,
      error: "Could not restore gradebook item",
    });
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-existing",
          contentId: assignmentItem.id,
          type: "Assignment",
        })}
      />,
    );

    const gradedSwitch = screen.getByRole("switch", { name: /^graded$/i });
    await user.click(gradedSwitch);
    await user.click(
      within(await screen.findByRole("alertdialog")).getByRole("button", {
        name: /remove grading/i,
      }),
    );
    await waitFor(() => expect(gradedSwitch).not.toBeChecked());

    await user.click(gradedSwitch);
    expect(
      await screen.findByText("Could not restore gradebook item"),
    ).toBeInTheDocument();
    expect(gradedSwitch).not.toBeChecked();
  });

  it("soft-deletes the linked assessment on confirm after toggling OFF", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-2",
          contentId: assignmentItem.id,
          type: "Assignment",
        })}
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

  it("shows removal failures and keeps grading enabled", async () => {
    const user = userEvent.setup();
    vi.mocked(deleteAssessment).mockResolvedValueOnce({
      success: false,
      error: "Could not remove gradebook item",
    });
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-2",
          contentId: assignmentItem.id,
          type: "Assignment",
        })}
      />,
    );

    const gradedSwitch = screen.getByRole("switch", { name: /^graded$/i });
    await user.click(gradedSwitch);
    await user.click(
      within(await screen.findByRole("alertdialog")).getByRole("button", {
        name: /remove grading/i,
      }),
    );

    expect(
      await screen.findByText("Could not remove gradebook item"),
    ).toBeInTheDocument();
    expect(gradedSwitch).toBeChecked();
  });

  it("does not soft-delete when the OFF confirm is cancelled", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={assignmentItem}
        courseTitle="Advanced Game AI"
        linkedAssessment={assessmentFixture({
          id: "asmnt-3",
          contentId: assignmentItem.id,
          type: "Assignment",
        })}
      />,
    );

    await user.click(screen.getByRole("switch", { name: /^graded$/i }));
    const dialog = await screen.findByRole("alertdialog");
    await user.click(within(dialog).getByRole("button", { name: /^cancel$/i }));

    expect(deleteAssessment).not.toHaveBeenCalled();
    expect(restoreAssessment).not.toHaveBeenCalled();
    expect(createAssessment).not.toHaveBeenCalled();
    // Switch state unchanged — still ON because no mutation ran.
    expect(screen.getByRole("switch", { name: /^graded$/i })).toBeChecked();
  });
});

// ── Task 10: reading-time estimation (auto hint / manual pin) ──

describe("ContentItemEditor — Reading-time estimation (Task 10)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(saveQuizAssessmentDraft).mockResolvedValue({
      success: true,
      data: {
        contentId: "content-1",
        contentVersion: 2,
        assessmentId: null,
        assessmentVersion: null,
      },
    });
    vi.mocked(createAssessment).mockResolvedValue({
      success: true,
      data: { id: "new-asmnt" },
    });
    vi.mocked(deleteAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(restoreAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(updateAssessment).mockResolvedValue({
      success: true,
      data: null,
    });
  });

  it("shows the live auto estimate as the duration placeholder for a Markdown lesson", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownAutoHint}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(screen.getByLabelText(/estimated minutes/i)).toHaveAttribute(
      "placeholder",
      "Auto (~2 min)",
    );
  });

  it("pins a manual estimate when a number is typed before saving", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownAutoHint}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.type(screen.getByLabelText(/estimated minutes/i), "15");
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          estimatedMinutes: 15,
          estimatedMinutesSource: "Manual",
        }),
      );
    });
  });

  it("sends a null estimate with Auto source when the field is left empty", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownAutoHint}
        courseTitle="Advanced Game AI"
      />,
    );

    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          estimatedMinutes: null,
          estimatedMinutesSource: "Auto",
        }),
      );
    });
  });

  it("reverts to Auto with a null estimate when a hydrated Manual value is cleared", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemManualDuration}
        courseTitle="Advanced Game AI"
      />,
    );

    const field = screen.getByLabelText(/estimated minutes/i);
    expect(field).toHaveValue(20);
    await user.clear(field);
    await user.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith(
        expect.objectContaining({
          estimatedMinutes: null,
          estimatedMinutesSource: "Auto",
        }),
      );
    });
  });

  it("hydrates the duration field only for Manual items and hides the auto helper text", () => {
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemManualDuration}
        courseTitle="Advanced Game AI"
      />,
    );

    expect(screen.getByLabelText(/estimated minutes/i)).toHaveValue(20);
    expect(
      screen.queryByText(/leave blank to keep auto/i),
    ).not.toBeInTheDocument();
  });
});
