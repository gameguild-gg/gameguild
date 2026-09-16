import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  replace: vi.fn(),
  pathname: "/workspace/testing-lab/events/event-1/participants" as string | null,
}));

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => mocks.pathname,
  useRouter: () => ({ replace: mocks.replace }),
}));

import { TestingParticipantFilters } from "./testing-participant-filters";

describe("TestingParticipantFilters", () => {
  beforeEach(() => {
    mocks.replace.mockReset();
    mocks.pathname = "/workspace/testing-lab/events/event-1/participants";
  });

  it("submits a normalized search while preserving status", async () => {
    const user = userEvent.setup();
    render(<TestingParticipantFilters search="old" status="Waitlisted" />);

    const input = screen.getByRole("textbox", { name: "Search participants" });
    await user.clear(input);
    await user.type(input, "  Ada Lovelace  ");
    await user.click(screen.getByRole("button", { name: "Search" }));

    expect(mocks.replace).toHaveBeenCalledWith(
      "/workspace/testing-lab/events/event-1/participants?q=Ada+Lovelace&status=Waitlisted",
    );
  });

  it("changes the status filter using the accessible selector", async () => {
    const user = userEvent.setup();
    render(<TestingParticipantFilters search="ada" />);

    await user.click(
      screen.getByRole("combobox", {
        name: "Filter participants by status",
      }),
    );
    await user.click(screen.getByRole("option", { name: "Checked in" }));
    expect(mocks.replace).toHaveBeenLastCalledWith(
      "/workspace/testing-lab/events/event-1/participants?q=ada&status=CheckedIn",
    );

  });

  it("removes the status while preserving search", async () => {
    const user = userEvent.setup();
    render(<TestingParticipantFilters search="ada" status="CheckedIn" />);

    await user.click(
      screen.getByRole("combobox", {
        name: "Filter participants by status",
      }),
    );
    await user.click(screen.getByRole("option", { name: "All statuses" }));

    expect(mocks.replace).toHaveBeenCalledWith(
      "/workspace/testing-lab/events/event-1/participants?q=ada",
    );
  });

  it("clears both filters and resets the visible search", async () => {
    const user = userEvent.setup();
    render(<TestingParticipantFilters search="ada" status="Completed" />);

    await user.click(
      screen.getByRole("button", { name: "Clear participant filters" }),
    );

    expect(screen.getByRole("textbox", { name: "Search participants" })).toHaveValue("");
    expect(mocks.replace).toHaveBeenCalledWith(
      "/workspace/testing-lab/events/event-1/participants",
    );
  });

  it("does not show a clear action without active filters", () => {
    render(<TestingParticipantFilters />);
    expect(
      screen.queryByRole("button", { name: "Clear participant filters" }),
    ).not.toBeInTheDocument();
  });
});
