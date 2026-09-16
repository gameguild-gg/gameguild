import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { scheduleFixture } from "./schedule-test-fixtures";
import { TimelineView } from "./timeline-view";

describe("TimelineView", () => {
  it("renders schedule events in chronological order", () => {
    render(<TimelineView schedule={scheduleFixture} />);

    const entries = screen.getAllByTestId("timeline-entry");
    expect(entries[0]).toHaveTextContent("Foundations");
    expect(entries[1]).toHaveTextContent("Foundations quiz");
    expect(entries[2]).toHaveTextContent("Foundations studio");
    expect(entries.at(-1)).toHaveTextContent("Decision systems");
  });

  it("renders the empty timeline state", () => {
    render(<TimelineView schedule={{ ...scheduleFixture, items: [] }} />);

    expect(
      screen.getByText("No timeline items have been scheduled."),
    ).toBeInTheDocument();
  });

  it("renders missing item data and suppresses duplicate due dates", () => {
    render(
      <TimelineView
        schedule={{
          ...scheduleFixture,
          items: [
            {
              type: "ContentRelease",
              instructionalWeek: 0,
              title: "   ",
            },
            {
              id: "same-due-date",
              type: "AssessmentWindow",
              instructionalWeek: undefined,
              sortOrder: undefined,
              availableFrom: "2026-09-01T12:00:00Z",
              dueAt: "2026-09-01T12:00:00Z",
              title: "Same-day assessment",
            },
            {
              id: "second-undated-item",
              type: "LiveSession",
              instructionalWeek: 1,
              sortOrder: undefined,
              title: "Second undated item",
            },
          ],
        }}
      />,
    );

    expect(screen.getAllByText("Date not set")).toHaveLength(2);
    expect(screen.getByText("Untitled schedule item")).toBeInTheDocument();
    expect(screen.getAllByText("Week 1")).toHaveLength(3);
    expect(screen.getByText("Same-day assessment")).toBeInTheDocument();
    expect(screen.getByText("Second undated item")).toBeInTheDocument();
    expect(screen.queryByText(/^Due /)).not.toBeInTheDocument();
  });
});
