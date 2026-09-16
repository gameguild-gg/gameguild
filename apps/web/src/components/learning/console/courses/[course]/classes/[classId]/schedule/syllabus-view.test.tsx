import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { scheduleFixture } from "./schedule-test-fixtures";
import { SyllabusView } from "./syllabus-view";

describe("SyllabusView", () => {
  it("groups items into instructional weeks", () => {
    render(<SyllabusView schedule={scheduleFixture} onShift={vi.fn()} />);

    expect(
      screen.getByRole("heading", { name: "Week 1 - Foundations" }),
    ).toBeVisible();
    expect(
      screen.getByRole("heading", { name: "Week 2 - Decision systems" }),
    ).toBeVisible();
    expect(screen.getByText("Available Aug 12, 08:00")).toBeVisible();
    expect(screen.getByText("Foundations quiz")).toBeVisible();
  });

  it("hides mutation controls when the class is read only", () => {
    render(
      <SyllabusView schedule={scheduleFixture} readOnly onShift={vi.fn()} />,
    );

    expect(
      screen.queryByRole("button", { name: /Shift Foundations/i }),
    ).not.toBeInTheDocument();
  });

  it("renders the empty syllabus state", () => {
    render(<SyllabusView schedule={{ ...scheduleFixture, items: [] }} />);

    expect(screen.getByText("No scheduled content yet")).toBeInTheDocument();
  });

  it("renders an undated milestone with safe week, title, and key fallbacks", () => {
    render(
      <SyllabusView
        schedule={{
          ...scheduleFixture,
          items: [
            {
              type: "Milestone",
              instructionalWeek: undefined,
              title: "   ",
            },
          ],
        }}
        onEdit={vi.fn()}
      />,
    );

    expect(
      screen.getByRole("heading", { name: "Week 1 - Instructional plan" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Untitled schedule item")).toBeInTheDocument();
    expect(screen.getByText("Date not set")).toBeInTheDocument();
    expect(screen.getByText("1 item")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /Edit Untitled schedule item/i }),
    ).not.toBeInTheDocument();
  });

  it("invokes edit and shift controls for persisted items", () => {
    const onEdit = vi.fn();
    const onShift = vi.fn();
    const { rerender } = render(
      <SyllabusView
        schedule={scheduleFixture}
        onEdit={onEdit}
        onShift={onShift}
      />,
    );

    screen.getByRole("button", { name: "Edit Foundations" }).click();
    screen.getByRole("button", { name: "Shift Foundations" }).click();

    expect(onEdit).toHaveBeenCalledWith(
      expect.objectContaining({ id: "release-1" }),
    );
    expect(onShift).toHaveBeenCalledWith(
      expect.objectContaining({ id: "release-1" }),
    );

    rerender(<SyllabusView schedule={scheduleFixture} onEdit={onEdit} />);
    expect(
      screen.queryByRole("button", { name: "Shift Foundations" }),
    ).not.toBeInTheDocument();
  });
});
