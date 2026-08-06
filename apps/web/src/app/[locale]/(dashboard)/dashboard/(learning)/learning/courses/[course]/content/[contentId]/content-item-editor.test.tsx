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
  content: "<p>Answer all questions.</p>",
  settings: { isRequired: true },
  lessonFormat: null,
  createdAt: "2026-01-01T00:00:00.000Z",
  updatedAt: "2026-01-02T00:00:00.000Z",
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
  content: "",
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
  content: "# existing markdown",
  settings: { isRequired: true },
  lessonFormat: "Markdown",
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

  it("saves edited quiz metadata and refreshes the dashboard route", async () => {
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
        body: "<p>Answer all questions.</p>",
        visibility: "Public",
        isRequired: true,
        estimatedMinutes: 35,
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText("Saved successfully.")).toBeInTheDocument();
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

  it("renders the lesson format selector for Lesson items and defaults to Markdown when body is empty", async () => {
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownEmpty}
        courseTitle="Advanced Game AI"
      />,
    );

    const trigger = screen.getByLabelText(/lesson format/i);
    expect(trigger).toHaveTextContent("Markdown");

    await user.click(trigger);
    const options = await screen.findAllByRole("option");
    expect(options).toHaveLength(6);
    expect(screen.getByRole("option", { name: /^markdown$/i })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: /^html$/i })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: /rich text \(lexical\)/i })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: /presentation \(revealjs\)/i })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: /video \(link\)/i })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: /^external link$/i })).toBeInTheDocument();
  });

  it("prompts for confirmation and keeps the current format when the user cancels a format change with non-empty body", async () => {
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();
    render(
      <ContentItemEditor
        courseId="course-1"
        item={lessonItemMarkdownBody}
        courseTitle="Advanced Game AI"
      />,
    );

    const trigger = screen.getByLabelText(/lesson format/i);
    expect(trigger).toHaveTextContent("Markdown");

    await user.click(trigger);
    await user.click(screen.getByRole("option", { name: /^html$/i }));

    expect(confirmSpy).toHaveBeenCalledWith(
      "Changing the lesson format will discard the current content. Continue?",
    );
    expect(trigger).toHaveTextContent("Markdown");

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
      expect(updateContent).toHaveBeenCalledWith({
        courseId: "course-1",
        contentId: "lesson-2",
        title: "Markdown lesson",
        description: "Has a Markdown body.",
        body: "# existing markdown",
        visibility: "Public",
        isRequired: true,
        estimatedMinutes: 15,
        lessonFormat: "Markdown",
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

    expect(
      screen.getByRole("button", { name: /^edit$/i }),
    ).toBeInTheDocument();
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
});
