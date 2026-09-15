import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { LearningCohortsCohortCalendarEntry } from "@game-guild/client";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";

import type { CourseCohortSummary } from "@/lib/learning/queries/cohorts";
import { GeneralCohortCalendar } from "./general-cohort-calendar";

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    href,
    children,
    ...props
  }: {
    href: string;
    children: ReactNode;
  }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

function cohort(id: string, name: string): CourseCohortSummary {
  return {
    id,
    courseId: "course-1",
    name,
    description: "",
    instructor: null,
    period: {
      startsAt: "2026-08-12T00:00:00Z",
      endsAt: "2026-12-18T00:00:00Z",
    },
    meetingPattern: null,
    enrollment: { current: 0, capacity: 24 },
    nextMeetingAt: null,
    conflictCount: 0,
    status: "scheduled",
    isOpen: false,
    schedule: null,
    createdAt: "2026-07-14T00:00:00Z",
  };
}

describe("GeneralCohortCalendar", () => {
  it("renders concurrent classes as separate calendar lanes", () => {
    const entries: LearningCohortsCohortCalendarEntry[] = [
      {
        cohortId: "morning",
        itemId: "item-1",
        title: "Module 1",
        type: "ContentRelease",
        startsAt: "2026-08-12T12:00:00Z",
      },
      {
        cohortId: "evening",
        itemId: "item-2",
        title: "Module 1",
        type: "ContentRelease",
        startsAt: "2026-08-14T22:00:00Z",
      },
    ];

    render(
      <GeneralCohortCalendar
        courseId="course-1"
        cohorts={[
          cohort("morning", "2026.2 - Morning"),
          cohort("evening", "2026.2 - Evening"),
        ]}
        entries={entries}
      />,
    );

    expect(
      screen.getByLabelText("2026.2 - Morning calendar lane"),
    ).toBeVisible();
    expect(
      screen.getByLabelText("2026.2 - Evening calendar lane"),
    ).toBeVisible();
    expect(screen.getAllByText("Module 1")).toHaveLength(2);
  });

  it("switches calendar density and renders empty cohort lanes", async () => {
    const user = userEvent.setup();
    const { container } = render(
      <GeneralCohortCalendar
        courseId="course-1"
        cohorts={[cohort("empty", "Empty class")]}
        entries={[]}
      />,
    );

    const calendar = container.querySelector("[data-calendar-mode]");
    expect(calendar).toHaveAttribute("data-calendar-mode", "week");
    expect(
      screen.getByText("Meeting pattern not configured"),
    ).toBeInTheDocument();
    expect(screen.getByText("0 scheduled items")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Month" }));
    expect(calendar).toHaveAttribute("data-calendar-mode", "month");
    await user.click(screen.getByRole("button", { name: "Week" }));
    expect(calendar).toHaveAttribute("data-calendar-mode", "week");
  });

  it("uses every schedule date fallback and safe item label", () => {
    const scheduled = {
      ...cohort("scheduled", "Scheduled class"),
      meetingPattern: "Tuesdays at 18:00 UTC",
    };
    const entries: LearningCohortsCohortCalendarEntry[] = [
      {
        cohortId: "scheduled",
        itemId: "starts",
        title: "Starts item",
        type: "LiveSession",
        startsAt: "2026-09-01T18:00:00Z",
      },
      {
        cohortId: "scheduled",
        itemId: "available",
        title: "Available item",
        type: "ContentRelease",
        availableFrom: "2026-09-02T18:00:00Z",
      },
      {
        cohortId: "scheduled",
        itemId: "due",
        title: "Due item",
        type: "AssessmentWindow",
        dueAt: "2026-09-03T18:00:00Z",
      },
      {
        cohortId: "scheduled",
        itemId: null,
        title: "",
        type: undefined,
      },
    ];

    render(
      <GeneralCohortCalendar
        courseId="course with spaces"
        cohorts={[scheduled]}
        entries={entries}
      />,
    );

    expect(screen.getByText("Tuesdays at 18:00 UTC")).toBeInTheDocument();
    expect(screen.getByText("Untitled schedule item")).toBeInTheDocument();
    expect(screen.getByText("Date not set")).toBeInTheDocument();
    expect(screen.getByText("Schedule item")).toBeInTheDocument();
    expect(screen.getByText("4 scheduled items")).toBeInTheDocument();
    expect(screen.getByText("Starts item").closest("a")).toHaveAttribute(
      "href",
      expect.stringContaining("#item-starts"),
    );
    expect(
      screen.getByText("Untitled schedule item").closest("a"),
    ).toHaveAttribute("href", expect.stringMatching(/#item-\d+$/));
  });
});
