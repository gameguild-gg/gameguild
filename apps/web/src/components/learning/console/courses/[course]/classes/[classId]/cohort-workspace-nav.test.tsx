import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import type { CourseCohortSummary } from "@/lib/learning/queries/cohorts";
import { CohortWorkspaceNav } from "./cohort-workspace-nav";

const push = vi.fn();
const pathname = vi.hoisted(() => ({
  value:
    "/workspace/learning/courses/advanced-game-ai-by-gameguild/classes/evening/schedule" as
      string | null,
}));

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
  usePathname: () => pathname.value,
  useRouter: () => ({ push }),
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
    meetingPattern: id === "morning" ? "Mon/Wed - 09:00" : "Tue/Thu - 19:00",
    enrollment: { current: 0, capacity: 24 },
    nextMeetingAt: null,
    conflictCount: 0,
    status: "scheduled",
    isOpen: false,
    schedule: null,
    createdAt: "2026-07-14T00:00:00Z",
  };
}

describe("CohortWorkspaceNav", () => {
  beforeEach(() => {
    push.mockReset();
    pathname.value =
      "/workspace/learning/courses/advanced-game-ai-by-gameguild/classes/evening/schedule";
  });

  it("switches classes without losing the course route", async () => {
    const user = userEvent.setup();
    const morning = cohort("morning", "2026.2 - Morning");
    const evening = cohort("evening", "2026.2 - Evening");

    render(
      <CohortWorkspaceNav
        courseRoute="advanced-game-ai-by-gameguild"
        courseTitle="Advanced Game AI"
        cohort={evening}
        cohorts={[morning, evening]}
      >
        <p>Workspace content</p>
      </CohortWorkspaceNav>,
    );

    await user.click(screen.getByRole("button", { name: "Switch class" }));
    await user.click(
      await screen.findByRole("menuitem", { name: /2026.2 - Morning/ }),
    );

    expect(push).toHaveBeenCalledWith(
      "/workspace/learning/courses/advanced-game-ai-by-gameguild/classes/morning/schedule",
    );
  });

  it("keeps the six class workspace sections visible", () => {
    const evening = cohort("evening", "2026.2 - Evening");
    render(
      <CohortWorkspaceNav
        courseRoute="course-1"
        courseTitle="Advanced Game AI"
        cohort={evening}
        cohorts={[evening]}
      >
        <p>Workspace content</p>
      </CohortWorkspaceNav>,
    );

    for (const label of [
      "Overview",
      "Schedule & content",
      "Students",
      "Assessments",
      "Gradebook",
      "Settings",
    ]) {
      expect(
        screen.getAllByRole("link", { name: label }).length,
      ).toBeGreaterThan(0);
    }
  });

  it("falls back to schedule navigation when the route and meeting pattern are absent", async () => {
    pathname.value = null;
    const flexible = {
      ...cohort("flexible", "Flexible class"),
      meetingPattern: null,
    };
    const user = userEvent.setup();

    render(
      <CohortWorkspaceNav
        courseRoute="course-1"
        courseTitle="Advanced Game AI"
        cohort={flexible}
        cohorts={[flexible]}
      >
        <p>Workspace content</p>
      </CohortWorkspaceNav>,
    );

    await user.click(screen.getByRole("button", { name: "Switch class" }));
    expect(
      await screen.findByText("Schedule not configured"),
    ).toBeInTheDocument();
    expect(screen.getByText("Workspace content")).toBeInTheDocument();
    expect(
      screen.getAllByRole("link", { name: "Schedule & content" })[0],
    ).toHaveClass("bg-muted");
  });
});
