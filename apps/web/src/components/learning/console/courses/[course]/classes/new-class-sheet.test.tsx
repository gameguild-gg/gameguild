import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { createCohort } from "@/lib/learning/actions/cohorts";
import { NewClassSheet } from "./new-class-sheet";

const refresh = vi.fn();
const push = vi.fn();

vi.mock("@/i18n/navigation", () => ({ useRouter: () => ({ refresh, push }) }));
vi.mock("@/lib/learning/actions/cohorts", () => ({ createCohort: vi.fn() }));

describe("NewClassSheet", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(createCohort).mockResolvedValue({
      success: true,
      data: { id: "cohort-1" },
    });
  });

  it("creates a cohort period and continues to its schedule", async () => {
    const user = userEvent.setup();
    render(<NewClassSheet courseId="course-1" />);

    await user.click(screen.getByRole("button", { name: "New class" }));
    fireEvent.change(screen.getByLabelText("Class name"), {
      target: { value: "2026.2 - Evening" },
    });
    fireEvent.change(screen.getByLabelText("Start date"), {
      target: { value: "2026-08-12" },
    });
    fireEvent.change(screen.getByLabelText("End date"), {
      target: { value: "2026-12-18" },
    });
    fireEvent.change(screen.getByLabelText("Capacity"), {
      target: { value: "24" },
    });
    fireEvent.change(screen.getByLabelText("Meeting pattern"), {
      target: { value: "Tue/Thu - 19:00" },
    });
    await user.click(
      screen.getByRole("button", { name: "Create and build schedule" }),
    );

    await waitFor(() => {
      expect(createCohort).toHaveBeenCalledWith(
        expect.objectContaining({
          courseId: "course-1",
          name: "2026.2 - Evening",
          startDate: "2026-08-12T00:00:00",
          endDate: "2026-12-18T23:59:59",
          maxCapacity: 24,
          meetingSchedule: "Tue/Thu - 19:00",
        }),
      );
    });
    expect(push).toHaveBeenCalledWith(
      "/workspace/learning/courses/course-1/classes/cohort-1/schedule",
    );
    expect(refresh).not.toHaveBeenCalled();
  });

  it("closes the sheet when creation is cancelled", async () => {
    const user = userEvent.setup();
    render(<NewClassSheet courseId="course-1" />);

    await user.click(screen.getByRole("button", { name: "New class" }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));

    await waitFor(() => {
      expect(
        screen.queryByRole("dialog", { name: "Create class" }),
      ).not.toBeInTheDocument();
    });
    expect(createCohort).not.toHaveBeenCalled();
  });

  it("keeps the sheet open and shows an API failure after the pending state", async () => {
    let finish:
      ((value: { success: false; error: string }) => void) | undefined;
    vi.mocked(createCohort).mockImplementation(
      () =>
        new Promise((resolve) => {
          finish = resolve;
        }),
    );
    const user = userEvent.setup();
    render(<NewClassSheet courseId="course-1" />);

    await user.click(screen.getByRole("button", { name: "New class" }));
    const form = screen
      .getByRole("dialog", { name: "Create class" })
      .querySelector("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form!);

    expect(
      await screen.findByRole("button", { name: "Create and build schedule" }),
    ).toBeDisabled();
    finish?.({ success: false, error: "The class could not be created." });

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The class could not be created.",
    );
    expect(screen.getByRole("dialog", { name: "Create class" })).toBeVisible();
    expect(push).not.toHaveBeenCalled();
  });

  it("normalizes missing form entries before invoking the action", async () => {
    const NativeFormData = globalThis.FormData;
    vi.stubGlobal(
      "FormData",
      class {
        get() {
          return null;
        }
      },
    );

    try {
      const user = userEvent.setup();
      render(<NewClassSheet courseId="course-1" />);
      await user.click(screen.getByRole("button", { name: "New class" }));
      const form = screen
        .getByRole("dialog", { name: "Create class" })
        .querySelector("form");
      expect(form).not.toBeNull();
      fireEvent.submit(form!);

      await waitFor(() => {
        expect(createCohort).toHaveBeenCalledWith({
          courseId: "course-1",
          name: "",
          description: "",
          startDate: "T00:00:00",
          endDate: "T23:59:59",
          maxCapacity: Number.NaN,
          meetingSchedule: "",
        });
      });
    } finally {
      vi.stubGlobal("FormData", NativeFormData);
    }
  });
});
