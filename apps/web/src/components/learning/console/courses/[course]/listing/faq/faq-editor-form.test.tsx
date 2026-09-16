import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FaqEditorForm } from "./faq-editor-form";

const updateCourseFaqMock = vi.fn();

vi.mock("@/lib/learning/actions", () => ({
  updateCourseFaq: (...args: unknown[]) => updateCourseFaqMock(...args),
}));

describe("FaqEditorForm", () => {
  beforeEach(() => {
    updateCourseFaqMock.mockReset();
    updateCourseFaqMock.mockResolvedValue({ success: true, data: null });
  });

  it("submits edited FAQ items to the course metadata action", async () => {
    render(
      <FaqEditorForm
        courseId="course-1"
        items={[
          {
            id: "course-1-faq-1",
            courseId: "course-1",
            question: "Who is this course for?",
            answer: "Intermediate game developers.",
            category: "Course details",
            order: 1,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-02T00:00:00.000Z",
          },
        ]}
      />,
    );

    fireEvent.change(screen.getByLabelText(/^question$/i), {
      target: { value: "What should students know first?" },
    });
    fireEvent.change(screen.getByLabelText(/^answer$/i), {
      target: {
        value:
          "Students should be comfortable with Unity scenes and C# scripts.",
      },
    });
    fireEvent.change(screen.getByLabelText(/^category$/i), {
      target: { value: "Prerequisites" },
    });

    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));

    await waitFor(() => {
      expect(updateCourseFaqMock).toHaveBeenCalledWith("course-1", [
        expect.objectContaining({
          id: "course-1-faq-1",
          question: "What should students know first?",
          answer:
            "Students should be comfortable with Unity scenes and C# scripts.",
          category: "Prerequisites",
        }),
      ]);
    });
    expect(screen.getByText("FAQ updated successfully.")).toBeInTheDocument();
  });

  it("supports empty FAQ setup, adding questions, removing down to one draft, and API errors", async () => {
    updateCourseFaqMock.mockResolvedValueOnce({
      success: false,
      error: "FAQ validation failed.",
    });

    render(<FaqEditorForm courseId="course-1" items={[]} />);

    expect(screen.getByText("Question 1")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/^question$/i), {
      target: { value: "Is there a certificate?" },
    });
    fireEvent.change(screen.getByLabelText(/^answer$/i), {
      target: { value: "Yes, after course completion." },
    });

    fireEvent.click(screen.getByRole("button", { name: /add question/i }));
    expect(screen.getByText("Question 2")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /remove question 2/i }));
    expect(screen.queryByText("Question 2")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /remove question 1/i }));
    expect(screen.getByText("Question 1")).toBeInTheDocument();
    expect(screen.getByLabelText(/^question$/i)).toHaveValue("");

    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));

    await waitFor(() => {
      expect(updateCourseFaqMock).toHaveBeenCalledWith("course-1", []);
    });
    expect(screen.getByText("FAQ validation failed.")).toBeInTheDocument();
  });

  it("uses the default category and persists an intentionally empty FAQ", async () => {
    const { rerender } = render(
      <FaqEditorForm
        courseId="course-1"
        items={[
          {
            id: "faq-without-category",
            courseId: "course-1",
            question: "Is support included?",
            answer: "Yes.",
            category: null,
            order: 1,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-02T00:00:00.000Z",
          },
        ]}
      />,
    );
    expect(screen.getByLabelText(/^category$/i)).toHaveValue("Course details");

    rerender(<FaqEditorForm key="empty" courseId="course-1" items={[]} />);
    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));

    await waitFor(() =>
      expect(updateCourseFaqMock).toHaveBeenCalledWith("course-1", []),
    );
    expect(screen.getByText("FAQ updated successfully.")).toBeInTheDocument();
    expect(screen.getAllByLabelText(/^question$/i)).toHaveLength(1);
  });

  it("surfaces typed and unknown exceptions and restores submission", async () => {
    updateCourseFaqMock
      .mockRejectedValueOnce(new Error("FAQ service unavailable."))
      .mockRejectedValueOnce("offline");
    render(<FaqEditorForm courseId="course-1" items={[]} />);

    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "FAQ service unavailable.",
    );
    await waitFor(() =>
      expect(screen.getByRole("button", { name: /save faq/i })).toBeEnabled(),
    );

    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));
    await waitFor(() =>
      expect(screen.getByRole("alert")).toHaveTextContent(
        "The FAQ could not be saved.",
      ),
    );
    expect(screen.getByRole("button", { name: /save faq/i })).toBeEnabled();
  });

  it("rejects partially completed FAQ entries before calling the API", async () => {
    render(<FaqEditorForm courseId="course-1" items={[]} />);

    fireEvent.change(screen.getByLabelText(/^question$/i), {
      target: { value: "Is mentoring included?" },
    });
    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Complete both the question and answer for Question 1.",
    );
    expect(updateCourseFaqMock).not.toHaveBeenCalled();
  });
  it("allows consecutive FAQ saves after editing and removing an entry", async () => {
    render(<FaqEditorForm courseId="course-1" items={[]} />);

    fireEvent.change(screen.getByLabelText(/^question$/i), {
      target: { value: "Primary question" },
    });
    fireEvent.change(screen.getByLabelText(/^answer$/i), {
      target: { value: "Primary answer." },
    });
    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));
    await waitFor(() =>
      expect(screen.getByRole("button", { name: /save faq/i })).toBeEnabled(),
    );

    fireEvent.click(screen.getByRole("button", { name: /add question/i }));
    fireEvent.change(screen.getAllByLabelText(/^question$/i).at(-1)!, {
      target: { value: "Temporary question" },
    });
    fireEvent.change(screen.getAllByLabelText(/^answer$/i).at(-1)!, {
      target: { value: "Temporary answer." },
    });
    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));
    await waitFor(() =>
      expect(screen.getByRole("button", { name: /save faq/i })).toBeEnabled(),
    );

    fireEvent.click(screen.getByRole("button", { name: /remove question 2/i }));
    fireEvent.click(screen.getByRole("button", { name: /save faq/i }));

    await waitFor(() => expect(updateCourseFaqMock).toHaveBeenCalledTimes(3));
    expect(updateCourseFaqMock).toHaveBeenLastCalledWith("course-1", [
      expect.objectContaining({ question: "Primary question" }),
    ]);
    expect(screen.getAllByLabelText(/^question$/i)).toHaveLength(1);
    expect(screen.getByRole("button", { name: /save faq/i })).toBeEnabled();
  });
});
