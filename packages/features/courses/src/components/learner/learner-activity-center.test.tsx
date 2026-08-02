import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { LearnerActivityCenter } from "./learner-activity-center";
import type { LearnerCourseRecord } from "./types";

const records: LearnerCourseRecord[] = [
  {
    course: {
      id: "course-1",
      title: "Game Production",
      slug: "game-production",
      description: "",
      thumbnail: null,
      modules: [
        {
          id: "module-1",
          title: "Community",
          description: "",
          order: 1,
          progress: 0,
          items: [
            {
              id: "discussion-1",
              title: "Production retrospective",
              type: "activity",
              contentType: "Discussion",
              status: "available",
              order: 1,
              isRequired: true,
            },
          ],
        },
      ],
      overallProgress: 0,
      totalItems: 1,
      completedItems: 0,
      remainingMinutes: 10,
    },
    context: {
      enrollmentId: "enrollment-1",
      cohort: null,
      calendar: [],
      assessmentGroups: [],
      assessments: [
        {
          id: "quiz-1",
          courseId: "course-1",
          title: "Game loop knowledge check",
          type: "Quiz",
          maxScore: 10,
        },
        {
          id: "project-1",
          courseId: "course-1",
          title: "Playable build",
          type: "Project",
          maxScore: 20,
        },
      ],
      submissions: [
        {
          id: "submission-1",
          assessmentId: "project-1",
          status: "Submitted",
        },
      ],
      discussions: [],
      certificates: [],
    },
  },
];

describe("LearnerActivityCenter", () => {
  it("shows assessment and participation work across enrolled courses", () => {
    render(<LearnerActivityCenter records={records} />);

    expect(
      screen.getByRole("heading", { name: "Assignments and activities" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Game loop knowledge check")).toBeInTheDocument();
    expect(screen.getByText("Playable build")).toBeInTheDocument();
    expect(screen.getByText("Production retrospective")).toBeInTheDocument();
  });

  it("filters activities by search and restores the complete list", () => {
    render(<LearnerActivityCenter records={records} />);

    fireEvent.change(
      screen.getByRole("textbox", { name: "Search activities" }),
      { target: { value: "retrospective" } },
    );
    expect(screen.getByText("Production retrospective")).toBeInTheDocument();
    expect(screen.queryByText("Playable build")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Clear filters" }));
    expect(screen.getByText("Playable build")).toBeInTheDocument();
    expect(screen.getByText("Game loop knowledge check")).toBeInTheDocument();
  });
});
