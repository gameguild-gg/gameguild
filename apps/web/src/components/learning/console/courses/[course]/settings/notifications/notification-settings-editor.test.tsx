import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ updateCourseNotificationSettings: vi.fn() }));

vi.mock("@/lib/learning/actions", () => ({
  updateCourseNotificationSettings: mocks.updateCourseNotificationSettings,
}));

import { NotificationSettingsEditor } from "./notification-settings-editor";

describe("NotificationSettingsEditor", () => {
  const settings = {
    courseId: "course-1",
    studentNotifications: {
      enrollmentConfirmation: true,
      courseUpdates: true,
      newContent: true,
      upcomingClasses: true,
      classReminders: [60],
      assignmentDue: true,
      assessmentResults: true,
      certificateReady: true,
      discussionReplies: true,
    },
    instructorNotifications: {
      newEnrollment: true,
      newReview: true,
      supportTicket: true,
      discussionMention: true,
      lowRating: true,
      lowRatingThreshold: 3,
    },
    templates: [
      { id: "welcome", type: "enrollment", subject: "Welcome", enabled: true },
    ],
    updatedAt: "2026-07-10T00:00:00.000Z",
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mocks.updateCourseNotificationSettings.mockResolvedValue({
      success: true,
      data: null,
    });
  });

  it("updates channels, reminders, threshold, and template subjects", async () => {
    const user = userEvent.setup();
    render(<NotificationSettingsEditor settings={settings} />);

    await user.click(screen.getByRole("switch", { name: "Course updates" }));
    fireEvent.change(screen.getByLabelText("Class reminder minutes"), {
      target: { value: "120, 15" },
    });
    fireEvent.change(screen.getByLabelText("Low rating threshold"), {
      target: { value: "2" },
    });
    fireEvent.change(screen.getByLabelText("Subject for enrollment"), {
      target: { value: "Welcome to production" },
    });
    await user.click(
      screen.getByRole("button", { name: "Save notification settings" }),
    );

    expect(mocks.updateCourseNotificationSettings).toHaveBeenCalledWith(
      "course-1",
      expect.objectContaining({
        studentNotifications: expect.objectContaining({
          courseUpdates: false,
          classReminders: [120, 15],
        }),
        instructorNotifications: expect.objectContaining({
          lowRatingThreshold: 2,
        }),
        templates: [
          expect.objectContaining({ subject: "Welcome to production" }),
        ],
      }),
    );
    expect(await screen.findByRole("status")).toHaveTextContent(
      "Notification settings saved.",
    );
  });

  it("updates instructor and template switches while preserving sibling templates", async () => {
    const user = userEvent.setup();
    render(
      <NotificationSettingsEditor
        settings={{
          ...settings,
          templates: [
            ...settings.templates,
            {
              id: "certificate",
              type: "certificate",
              subject: "Ready",
              enabled: false,
            },
          ],
        }}
      />,
    );

    await user.click(screen.getByRole("switch", { name: "Low rating alert" }));
    fireEvent.change(screen.getByLabelText("Subject for enrollment"), {
      target: { value: "Updated welcome" },
    });
    await user.click(
      screen.getByRole("switch", { name: "Enable enrollment template" }),
    );
    await user.click(
      screen.getByRole("button", { name: "Save notification settings" }),
    );

    expect(mocks.updateCourseNotificationSettings).toHaveBeenCalledWith(
      "course-1",
      expect.objectContaining({
        instructorNotifications: expect.objectContaining({ lowRating: false }),
        templates: [
          expect.objectContaining({
            id: "welcome",
            enabled: false,
            subject: "Updated welcome",
          }),
          expect.objectContaining({ id: "certificate", enabled: false }),
        ],
      }),
    );
  });

  it("drops invalid reminder tokens and reports persistence failures", async () => {
    mocks.updateCourseNotificationSettings.mockResolvedValueOnce({
      success: false,
      error: "Notification settings were rejected.",
    });
    const user = userEvent.setup();
    render(<NotificationSettingsEditor settings={settings} />);

    fireEvent.change(screen.getByLabelText("Class reminder minutes"), {
      target: { value: "120, invalid; 15" },
    });
    await user.click(
      screen.getByRole("button", { name: "Save notification settings" }),
    );

    expect(mocks.updateCourseNotificationSettings).toHaveBeenCalledWith(
      "course-1",
      expect.objectContaining({
        studentNotifications: expect.objectContaining({
          classReminders: [120, 15],
        }),
      }),
    );
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Notification settings were rejected.",
    );
  });

  it("disables save while notification persistence is pending", async () => {
    let resolveSave!: (value: { success: true; data: null }) => void;
    mocks.updateCourseNotificationSettings.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveSave = resolve;
      }),
    );
    const user = userEvent.setup();
    render(<NotificationSettingsEditor settings={settings} />);

    const save = screen.getByRole("button", {
      name: "Save notification settings",
    });
    await user.click(save);
    await waitFor(() => expect(save).toBeDisabled());
    resolveSave({ success: true, data: null });

    expect(await screen.findByRole("status")).toHaveTextContent(
      "Notification settings saved.",
    );
  });
});
