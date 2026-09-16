import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";

import type { CourseCohortSummary } from "@/lib/learning/queries/cohorts";
import { ClassControlCenter } from "./class-control-center";

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
  useRouter: () => ({ refresh: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/learning/actions/cohorts", () => ({
  createCohort: vi.fn(),
}));

function cohort(overrides: Partial<CourseCohortSummary>): CourseCohortSummary {
  return {
    id: "cohort-1",
    courseId: "course-1",
    name: "2026.2 - Morning",
    description: "",
    instructor: null,
    period: {
      startsAt: "2026-08-12T00:00:00Z",
      endsAt: "2026-12-18T00:00:00Z",
    },
    meetingPattern: "Mon/Wed - 09:00",
    enrollment: { current: 8, capacity: 24 },
    nextMeetingAt: "2026-08-12T12:00:00Z",
    conflictCount: 0,
    status: "scheduled",
    isOpen: true,
    schedule: null,
    createdAt: "2026-07-14T00:00:00Z",
    ...overrides,
  };
}

describe("ClassControlCenter", () => {
  it("shows independent morning and evening classes", () => {
    render(
      <ClassControlCenter
        courseId="course-1"
        cohorts={[
          cohort({
            id: "morning",
            name: "2026.2 - Morning",
            meetingPattern: "Mon/Wed - 09:00",
          }),
          cohort({
            id: "evening",
            name: "2026.2 - Evening",
            meetingPattern: "Tue/Thu - 19:00",
          }),
        ]}
      />,
    );

    expect(screen.getAllByText("2026.2 - Morning").length).toBeGreaterThan(0);
    expect(screen.getAllByText("2026.2 - Evening").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Mon/Wed - 09:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Tue/Thu - 19:00").length).toBeGreaterThan(0);
  });

  it("opens class creation in a sheet", async () => {
    const user = userEvent.setup();
    render(<ClassControlCenter courseId="course-1" cohorts={[]} />);

    expect(screen.queryByLabelText("Class name")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "New class" }));

    expect(screen.getByRole("dialog", { name: "Create class" })).toBeVisible();
    expect(screen.getByLabelText("Class name")).toBeVisible();
  });

  it("renders every class status and schedule fallback", () => {
    render(
      <ClassControlCenter
        courseId="course-1"
        cohorts={[
          cohort({ id: "active", name: "Active class", status: "active" }),
          cohort({
            id: "completed",
            name: "Completed class",
            status: "completed",
          }),
          cohort({
            id: "cancelled",
            name: "Cancelled class",
            status: "cancelled",
          }),
          cohort({
            id: "scheduled",
            name: "Flexible class",
            status: "scheduled",
            meetingPattern: null,
            nextMeetingAt: null,
            enrollment: { current: 3, capacity: null },
          }),
        ]}
      />,
    );

    expect(screen.getAllByText("Active").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Completed").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Cancelled").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Scheduled").length).toBeGreaterThan(0);
    expect(screen.getByText("Not configured")).toBeInTheDocument();
    expect(screen.getByText("Schedule not configured")).toBeInTheDocument();
    expect(screen.getByText("Not scheduled")).toBeInTheDocument();
    expect(screen.getAllByText("3/Unlimited")).toHaveLength(2);
  });

  it("filters classes by name and meeting pattern", async () => {
    const user = userEvent.setup();
    render(
      <ClassControlCenter
        courseId="course-1"
        cohorts={[
          cohort({
            id: "morning",
            name: "Morning class",
            meetingPattern: "Mon/Wed",
          }),
          cohort({
            id: "evening",
            name: "Evening class",
            meetingPattern: null,
          }),
        ]}
      />,
    );

    await user.type(
      screen.getByRole("textbox", { name: "Search classes" }),
      "mon/wed",
    );

    expect(screen.getByText("1 of 2")).toBeInTheDocument();
    expect(screen.getAllByText("Morning class").length).toBeGreaterThan(0);
    expect(screen.queryByText("Evening class")).not.toBeInTheDocument();

    await user.clear(screen.getByRole("textbox", { name: "Search classes" }));
    await user.type(
      screen.getByRole("textbox", { name: "Search classes" }),
      "missing",
    );

    expect(screen.getByText("No classes found")).toBeInTheDocument();
    expect(screen.getByText("0 of 2")).toBeInTheDocument();
  });

  it("filters classes by status", async () => {
    const user = userEvent.setup();
    render(
      <ClassControlCenter
        courseId="course-1"
        cohorts={[
          cohort({ id: "active", name: "Active class", status: "active" }),
          cohort({
            id: "scheduled",
            name: "Scheduled class",
            status: "scheduled",
          }),
        ]}
      />,
    );

    await user.click(
      screen.getByRole("combobox", { name: "Filter class status" }),
    );
    await user.click(screen.getByRole("option", { name: "Active" }));

    expect(screen.getByText("1 of 2")).toBeInTheDocument();
    expect(screen.getAllByText("Active class").length).toBeGreaterThan(0);
    expect(screen.queryByText("Scheduled class")).not.toBeInTheDocument();
  });
});
