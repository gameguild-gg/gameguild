import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  addCourseSupportTicketMessage,
  resolveCourseSupportTicket,
} from "@/lib/learning/actions";
import { CourseTicketActionPanel } from "./course-ticket-action-panel";

const refresh = vi.fn();
vi.mock("next/navigation", () => ({
  usePathname: () => "/workspace/learning",
  useRouter: () => ({ refresh }),
}));
vi.mock("@/lib/learning/actions", () => ({
  addCourseSupportTicketMessage: vi.fn(),
  resolveCourseSupportTicket: vi.fn(),
}));

describe("CourseTicketActionPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(addCourseSupportTicketMessage).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(resolveCourseSupportTicket).mockResolvedValue({
      success: true,
      data: null,
    });
  });

  it("replies to a persisted support ticket", async () => {
    const user = userEvent.setup();
    render(
      <CourseTicketActionPanel
        courseId="course-1"
        ticketId="ticket-1"
        resolved={false}
      />,
    );

    fireEvent.change(screen.getByLabelText("Reply"), {
      target: { value: "Please retry after refreshing the lesson." },
    });
    await user.click(screen.getByRole("button", { name: "Send reply" }));

    await waitFor(() =>
      expect(addCourseSupportTicketMessage).toHaveBeenCalledWith({
        courseId: "course-1",
        ticketId: "ticket-1",
        message: "Please retry after refreshing the lesson.",
      }),
    );
    expect(await screen.findByRole("status")).toHaveTextContent("Reply sent.");
  });

  it("resolves a ticket with a required resolution summary", async () => {
    const user = userEvent.setup();
    render(
      <CourseTicketActionPanel
        courseId="course-1"
        ticketId="ticket-1"
        resolved={false}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Resolve ticket" }));
    fireEvent.change(screen.getByLabelText("Resolution summary"), {
      target: { value: "Access entitlement was refreshed." },
    });
    await user.click(
      screen.getByRole("button", { name: "Confirm resolution" }),
    );

    await waitFor(() =>
      expect(resolveCourseSupportTicket).toHaveBeenCalledWith({
        courseId: "course-1",
        ticketId: "ticket-1",
        summary: "Access entitlement was refreshed.",
      }),
    );
  });

  it("keeps a failed reply available for retry", async () => {
    vi.mocked(addCourseSupportTicketMessage).mockResolvedValue({
      success: false,
      error: "Reply delivery failed.",
    });
    const user = userEvent.setup();
    render(
      <CourseTicketActionPanel
        courseId="course-1"
        ticketId="ticket-1"
        resolved={false}
      />,
    );

    fireEvent.change(screen.getByLabelText("Reply"), {
      target: { value: " Please try again. " },
    });
    await user.click(screen.getByRole("button", { name: "Send reply" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Reply delivery failed.",
    );
    expect(screen.getByLabelText("Reply")).toHaveValue(" Please try again. ");
    expect(refresh).not.toHaveBeenCalled();
  });

  it("keeps the resolution dialog open after a failed resolution", async () => {
    vi.mocked(resolveCourseSupportTicket).mockResolvedValue({
      success: false,
      error: "Resolution failed.",
    });
    const user = userEvent.setup();
    render(
      <CourseTicketActionPanel
        courseId="course-1"
        ticketId="ticket-1"
        resolved={false}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Resolve ticket" }));
    fireEvent.change(screen.getByLabelText("Resolution summary"), {
      target: { value: " Still needs investigation. " },
    });
    await user.click(
      screen.getByRole("button", { name: "Confirm resolution" }),
    );

    expect(await screen.findByText("Resolution failed.")).toBeInTheDocument();
    expect(
      screen.getByRole("dialog", { name: "Resolve ticket" }),
    ).toBeVisible();
    expect(screen.getByLabelText("Resolution summary")).toHaveValue(
      " Still needs investigation. ",
    );
    expect(refresh).not.toHaveBeenCalled();
  });

  it("cancels resolution and disables actions for a resolved ticket", async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <CourseTicketActionPanel
        courseId="course-1"
        ticketId="ticket-1"
        resolved={false}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Resolve ticket" }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() =>
      expect(
        screen.queryByRole("dialog", { name: "Resolve ticket" }),
      ).not.toBeInTheDocument(),
    );

    rerender(
      <CourseTicketActionPanel
        courseId="course-1"
        ticketId="ticket-1"
        resolved
      />,
    );
    expect(screen.getByLabelText("Reply")).toBeDisabled();
    expect(screen.getByRole("button", { name: "Send reply" })).toBeDisabled();
    expect(
      screen.getByRole("button", { name: "Resolve ticket" }),
    ).toBeDisabled();
  });
});
