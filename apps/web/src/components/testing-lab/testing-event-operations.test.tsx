import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
Element.prototype.scrollIntoView = vi.fn();

const actions = vi.hoisted(() => ({
  addTestingEventCommitteeMember: vi.fn(),
  archiveTestingEvent: vi.fn(),
  assignTestedProjectToRegistration: vi.fn(),
  approveTestingEventApplication: vi.fn(),
  beginTestingEventApplicationReview: vi.fn(),
  configureTestingEventLearning: vi.fn(),
  createTestingEvent: vi.fn(),
  createTestingEventSlot: vi.fn(),
  deleteTestingEvent: vi.fn(),
  deleteTestingEventSlot: vi.fn(),
  rejectTestingEventApplication: vi.fn(),
  removeTestingEventCommitteeMember: vi.fn(),
  restoreTestingEvent: vi.fn(),
  transitionTestingEvent: vi.fn(),
  updateTestingEvent: vi.fn(),
  updateTestingEventAttendance: vi.fn(),
  updateTestingEventSlot: vi.fn(),
  voteOnTestingEventApplication: vi.fn(),
  waitlistTestingEventApplication: vi.fn(),
}));
const router = vi.hoisted(() => ({ push: vi.fn(), refresh: vi.fn() }));

vi.mock("@/lib/testing-lab/events-actions", () => actions);
vi.mock("next/navigation", () => ({ useRouter: () => router }));

import {
  RestoreTestingEventDialog,
  TestingEventCommittee,
  TestingEventLearningDialog,
  TestingSlotRegistrations,
} from "./testing-event-management";

const success = { success: true as const, data: null, message: "Saved." };
const realSetTimeout = window.setTimeout.bind(window);

async function choose(
  user: ReturnType<typeof userEvent.setup>,
  label: string,
  option: string,
) {
  await user.click(screen.getByRole("combobox", { name: label }));
  await user.click(await screen.findByRole("option", { name: option }));
}

describe("Testing Lab event operations", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.values(actions).forEach((action) => action.mockResolvedValue(success));
    vi.spyOn(window, "setTimeout").mockImplementation((handler, timeout, ...args) =>
      realSetTimeout(handler, timeout === 450 ? 0 : timeout, ...args),
    );
  });

  afterEach(() => vi.restoreAllMocks());

  it("adds and removes review committee members", async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <TestingEventCommittee
        event={{ id: "event-1", approvalMode: "Committee" }}
        members={[{ id: "user-1", label: "Alex Reviewer" }]}
        committee={[]}
      />,
    );

    expect(screen.getByText(/Committee votes inform approval/)).toBeInTheDocument();
    expect(screen.getByText("No reviewers assigned.")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Add reviewer" }));
    await choose(user, "Committee member", "Alex Reviewer");
    await user.click(screen.getByRole("checkbox", { name: "Committee chair" }));
    await user.click(screen.getByRole("button", { name: "Add reviewer" }));
    await waitFor(() =>
      expect(actions.addTestingEventCommitteeMember).toHaveBeenCalledOnce(),
    );

    rerender(
      <TestingEventCommittee
        event={{ id: "event-1", approvalMode: "ManagerOnly" }}
        members={[]}
        committee={[
          {
            id: "reviewer-1",
            userId: "user-1",
            userName: "Alex Reviewer",
            userEmail: "alex@example.test",
            isChair: true,
          },
          {
            id: undefined,
            userId: "user-2",
            userName: undefined,
            userEmail: "sam@example.test",
            isChair: false,
          },
          {
            id: "reviewer-3",
            userId: "user-3",
            userName: undefined,
            userEmail: undefined,
            isChair: false,
          },
        ]}
      />,
    );

    expect(screen.getByText("Manager-only approval is active.")).toBeInTheDocument();
    expect(screen.getByText("Chair")).toBeInTheDocument();
    expect(screen.getAllByText("sam@example.test")).toHaveLength(2);
    expect(screen.getByText("user-3")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Remove Alex Reviewer" }));
    await user.click(screen.getByRole("button", { name: "Remove reviewer" }));
    await waitFor(() =>
      expect(actions.removeTestingEventCommitteeMember).toHaveBeenCalledOnce(),
    );
  });

  it("keeps committee controls hidden in read-only and unidentified events", () => {
    const { rerender } = render(
      <TestingEventCommittee
        event={{ id: "event-1", approvalMode: "ManagerOnly" }}
        members={[]}
        committee={[{ id: "reviewer-1", userId: "user-1", userName: "Alex" }]}
        readOnly
      />,
    );
    expect(screen.queryByRole("button")).not.toBeInTheDocument();

    rerender(
      <TestingEventCommittee
        event={{ approvalMode: "Committee" }}
        members={[]}
        committee={[]}
      />,
    );
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("restores an archived event and ignores malformed projections", async () => {
    const user = userEvent.setup();
    const { rerender } = render(<RestoreTestingEventDialog event={{}} />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();

    rerender(<RestoreTestingEventDialog event={{ id: "event-1" }} />);
    await user.click(screen.getByRole("button", { name: "Restore event" }));
    await user.click(screen.getByRole("button", { name: "Restore event" }));
    await waitFor(() => expect(actions.restoreTestingEvent).toHaveBeenCalledOnce());
    expect(router.refresh).toHaveBeenCalledOnce();
  });

  it("shows the empty registration state", () => {
    render(
      <TestingSlotRegistrations
        eventId="event-1"
        registrations={[]}
        memberLabels={{}}
        approvedApplications={[]}
      />,
    );
    expect(screen.getByText("No tester registrations.")).toBeInTheDocument();
  });

  it("assigns eligible projects and updates attendance", async () => {
    const user = userEvent.setup();
    const registrations = [
      {
        id: "registration-1",
        userId: "user-1",
        slotId: "slot-1",
        status: "CheckedIn",
        pendingFeedbackCount: 2,
      },
      {
        id: "registration-2",
        userId: "user-2",
        slotId: "slot-2",
        status: "Scheduled",
        pendingFeedbackCount: undefined,
      },
      {
        id: "registration-3",
        userId: undefined,
        slotId: "slot-1",
        status: undefined,
      },
    ];
    render(
      <TestingSlotRegistrations
        eventId="event-1"
        registrations={registrations}
        memberLabels={{ "user-1": "Alex Tester", "user-2": "Sam Tester" }}
        approvedApplications={[
          {
            id: "application-global",
            label: "Global project",
            slotId: null,
            eligibleTesterUserIds: ["user-1"],
          },
          {
            id: "application-slot",
            label: "Slot project",
            slotId: "slot-1",
            eligibleTesterUserIds: ["user-1"],
          },
          {
            id: "application-other",
            label: "Other slot",
            slotId: "slot-9",
            eligibleTesterUserIds: ["user-1"],
          },
        ]}
      />,
    );

    expect(screen.getByText("Alex Tester")).toBeInTheDocument();
    expect(screen.getByText("Unknown tester")).toBeInTheDocument();
    expect(screen.getByText(/2 pending feedback/)).toBeInTheDocument();
    expect(screen.getAllByText(/0 pending feedback/)).not.toHaveLength(0);

    await user.click(screen.getByRole("button", { name: "Assign tested project" }));
    await choose(user, "Approved project", "Slot project");
    expect(screen.queryByRole("option", { name: "Other slot" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Assign project" }));
    await waitFor(() =>
      expect(actions.assignTestedProjectToRegistration).toHaveBeenCalledOnce(),
    );

    const attendance = screen.getAllByRole("combobox", { name: "Attendance" })[0]!;
    await user.click(attendance);
    await user.click(await screen.findByRole("option", { name: "Check in" }));
    const attendanceForm = attendance.closest("form")!;
    fireEvent.submit(attendanceForm);
    await waitFor(() =>
      expect(actions.updateTestingEventAttendance).toHaveBeenCalledOnce(),
    );
  });

  it("hides mutation controls for terminal, read-only, and unidentified registrations", () => {
    render(
      <TestingSlotRegistrations
        eventId="event-1"
        registrations={[
          { id: "cancelled", userId: "u1", status: "Cancelled", pendingFeedbackCount: 3 },
          { id: "completed", userId: "u2", status: "Completed", pendingFeedbackCount: 3 },
          { id: "no-show", userId: "u3", status: "NoShow", pendingFeedbackCount: 3 },
          { id: undefined, userId: "u4", status: "Scheduled" },
          { id: "read-only", userId: "u5", status: "Attended" },
        ]}
        memberLabels={{}}
        approvedApplications={[]}
        readOnly
      />,
    );
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
    expect(screen.getAllByText(/0 pending feedback/)).toHaveLength(5);
  });

  it("configures learning evidence and updates its course with the activity", async () => {
    const user = userEvent.setup();
    const activities = [
      { id: "activity-1", courseId: "course-1", label: "Lesson one" },
      { id: "activity-2", courseId: "course-2", label: "Final quiz" },
    ];
    render(
      <TestingEventLearningDialog
        event={{
          id: "event-1",
          learningActivityId: "activity-1",
          cohortId: "cohort-1",
          learningCompletionRequirement: "ProjectTested",
        }}
        activities={activities}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Configure learning" }));
    expect(document.querySelector<HTMLInputElement>('input[name="courseId"]')).toHaveValue("course-1");
    expect(screen.getByLabelText("Cohort id")).toHaveValue("cohort-1");
    expect(screen.getByRole("combobox", { name: "Completion requirement" })).toHaveTextContent(
      "Assigned project tested",
    );

    await choose(user, "Course activity", "Final quiz");
    expect(document.querySelector<HTMLInputElement>('input[name="courseId"]')).toHaveValue("course-2");
    await user.click(screen.getByRole("button", { name: "Save learning link" }));
    await waitFor(() =>
      expect(actions.configureTestingEventLearning).toHaveBeenCalledOnce(),
    );
  });

  it("uses learning fallbacks and hides configuration when unavailable", async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <TestingEventLearningDialog
        event={{ id: "event-1", courseId: "course-fallback" }}
        activities={[]}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Configure learning" }));
    expect(document.querySelector<HTMLInputElement>('input[name="courseId"]')).toHaveValue(
      "course-fallback",
    );
    expect(screen.getByLabelText("Cohort id")).toHaveValue("");
    expect(screen.getByRole("combobox", { name: "Completion requirement" })).toHaveTextContent(
      "Attendance and feedback",
    );

    rerender(
      <TestingEventLearningDialog event={{ id: "event-1" }} activities={[]} readOnly />,
    );
    expect(screen.queryByRole("button", { name: "Configure learning" })).not.toBeInTheDocument();
    rerender(<TestingEventLearningDialog event={{}} activities={[]} />);
    expect(screen.queryByRole("button", { name: "Configure learning" })).not.toBeInTheDocument();
  });
});
