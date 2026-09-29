import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";

import { DateTimeRangePicker } from "./date-time-range-picker";

function RangeForm() {
  return (
    <form aria-label="Session dates">
      <DateTimeRangePicker
        id="session"
        label="Testing session"
        startLabel="Session starts"
        endLabel="Session ends"
        startName="startsAt"
        endName="endsAt"
        timeZoneId="America/Sao_Paulo"
        defaultValue={{ start: "2026-10-02T18:00", end: "2026-10-03T21:00" }}
        required
      />
    </form>
  );
}

describe("DateTimeRangePicker", () => {
  it("shows both full endpoints even when a compact interval starts and ends on the same day", () => {
    render(<DateTimeRangePicker
      id="compact-session" label="Testing session" startLabel="Session starts" endLabel="Session ends"
      startName="startsAt" endName="endsAt" timeZoneId="America/Sao_Paulo" compact
      defaultValue={{ start: "2026-10-02T18:00", end: "2026-10-02T21:00" }}
    />);

    expect(screen.getByRole("button", { name: "Testing session" }))
      .toHaveTextContent("02/10/2026 · 18:00 → 02/10/2026 · 21:00");
  });

  it("keeps a compact interval closed until the calendar is requested", async () => {
    const user = userEvent.setup();
    render(<DateTimeRangePicker
      id="compact-session" label="Testing session" startLabel="Session starts" endLabel="Session ends"
      startName="startsAt" endName="endsAt" timeZoneId="America/Sao_Paulo" compact
      defaultValue={{ start: "2026-10-02T18:00", end: "2026-10-03T21:00" }}
    />);

    const trigger = screen.getByRole("button", { name: "Testing session" });
    expect(trigger).toHaveTextContent("02/10/2026 · 18:00 → 03/10/2026 · 21:00");
    expect(screen.queryByLabelText("Session starts time")).not.toBeInTheDocument();
    await user.click(trigger);
    expect(await screen.findByLabelText("Session starts time")).toHaveValue("18:00");
    expect(screen.getByLabelText("Session ends time")).toHaveValue("21:00");
    expect(screen.getByText("America/Sao_Paulo · 24-hour clock")).toBeInTheDocument();
  });

  it("applies compact edits only on confirmation and cancels without changing the interval", async () => {
    const user = userEvent.setup();
    render(<form aria-label="Compact session"><DateTimeRangePicker
      id="compact-session" label="Testing session" startLabel="Session starts" endLabel="Session ends"
      startName="startsAt" endName="endsAt" timeZoneId="America/Sao_Paulo" compact required
      defaultValue={{ start: "2026-10-02T18:00", end: "2026-10-03T21:00" }}
    /></form>);
    const values = () => new FormData(screen.getByRole("form", { name: "Compact session" }) as HTMLFormElement);
    await user.click(screen.getByRole("button", { name: "Testing session" }));
    fireEvent.change(await screen.findByLabelText("Session starts time"), { target: { value: "23:15" } });
    expect(values().get("startsAt")).toBe("2026-10-02T18:00");
    await user.click(screen.getByRole("button", { name: "Cancel", exact: true }));
    await waitFor(() => expect(screen.queryByLabelText("Session starts time")).not.toBeInTheDocument());
    expect(values().get("startsAt")).toBe("2026-10-02T18:00");
    await user.click(screen.getByRole("button", { name: "Testing session" }));
    expect(await screen.findByLabelText("Session starts time")).toHaveValue("18:00");
    fireEvent.change(screen.getByLabelText("Session starts time"), { target: { value: "23:15" } });
    await user.click(screen.getByRole("button", { name: "Apply testing session" }));
    expect(values().get("startsAt")).toBe("2026-10-02T23:15");
    expect(values().get("endsAt")).toBe("2026-10-03T21:00");
  });

  it("exposes a complete date and 24-hour time for both endpoints", () => {
    render(<RangeForm />);

    expect(screen.getByLabelText("Session starts date")).toHaveValue("2026-10-02");
    expect(screen.getByLabelText("Session starts time")).toHaveValue("18:00");
    expect(screen.getByLabelText("Session ends date")).toHaveValue("2026-10-03");
    expect(screen.getByLabelText("Session ends time")).toHaveValue("21:00");
    expect(screen.getByRole("group", { name: "Session starts" })).toHaveClass("@container/endpoint");
  });

  it.each([
    { endDate: "Friday, October 9th, 2026", end: "2026-10-09T01:15", summary: "09/10/2026 · 01:15" },
    { endDate: "Sunday, October 11th, 2026", end: "2026-10-11T01:15", summary: "11/10/2026 · 01:15" },
  ])("keeps a full calendar interval ending on $endDate", async ({ endDate, end, summary }) => {
    const user = userEvent.setup();
    render(<form aria-label="Compact session"><DateTimeRangePicker
      id="compact-session" label="Testing session" startLabel="Session starts" endLabel="Session ends"
      startName="startsAt" endName="endsAt" timeZoneId="America/Sao_Paulo" compact
      defaultValue={{ start: "2026-10-02T18:00", end: "2026-10-03T21:00" }}
    /></form>);
    await user.click(screen.getByRole("button", { name: "Testing session" }));
    await user.click(await screen.findByRole("button", { name: "Thursday, October 8th, 2026", exact: true }));
    expect(screen.getAllByText("08/10/2026")).toHaveLength(2);
    await user.click(screen.getByRole("button", { name: endDate, exact: true }));
    fireEvent.change(screen.getByLabelText("Session starts time"), { target: { value: "23:30" } });
    fireEvent.change(screen.getByLabelText("Session ends time"), { target: { value: "01:15" } });
    await user.click(screen.getByRole("button", { name: "Apply testing session" }));
    const data = new FormData(screen.getByRole("form", { name: "Compact session" }) as HTMLFormElement);
    expect(data.get("startsAt")).toBe("2026-10-08T23:30");
    expect(data.get("endsAt")).toBe(end);
    expect(screen.getByRole("button", { name: "Testing session" })).toHaveTextContent(`08/10/2026 · 23:30 → ${summary}`);
  });

  it("does not apply invalid 24-hour times, reversed intervals, or nonexistent DST times", async () => {
    const user = userEvent.setup();
    render(<form aria-label="Compact session"><DateTimeRangePicker
      id="compact-session" label="Testing session" startLabel="Session starts" endLabel="Session ends"
      startName="startsAt" endName="endsAt" timeZoneId="America/New_York" compact
      defaultValue={{ start: "2026-03-08T01:30", end: "2026-03-08T03:30" }}
    /></form>);
    await user.click(screen.getByRole("button", { name: "Testing session" }));
    for (const [time, message] of [
      ["24:00", "Enter both times as HH:MM (24-hour)."],
      ["04:00", "Session ends must be after session starts."],
      ["02:30", "Choose valid dates and times in America/New_York."],
    ]) {
      fireEvent.change(await screen.findByLabelText("Session starts time"), { target: { value: time } });
      await user.click(screen.getByRole("button", { name: "Apply testing session" }));
      expect(screen.getByRole("alert")).toHaveTextContent(message!);
      const data = new FormData(screen.getByRole("form", { name: "Compact session" }) as HTMLFormElement);
      expect(data.get("startsAt")).toBe("2026-03-08T01:30");
    }
  });

  it("keeps the other endpoint unchanged and submits full local datetimes", () => {
    render(<RangeForm />);
    fireEvent.change(screen.getByLabelText("Session starts date"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("Session starts time"), { target: { value: "23:15" } });

    const data = new FormData(screen.getByRole("form", { name: "Session dates" }) as HTMLFormElement);
    expect(data.get("startsAt")).toBe("2026-10-01T23:15");
    expect(data.get("endsAt")).toBe("2026-10-03T21:00");
  });

  it("rejects ambiguous or invalid time text through native form validation", () => {
    render(<RangeForm />);
    const time = screen.getByLabelText("Session starts time") as HTMLInputElement;
    for (const value of ["6:00", "24:00", "18:60", "06 PM"]) {
      fireEvent.change(time, { target: { value } });
      expect(time.checkValidity()).toBe(false);
    }
    fireEvent.change(time, { target: { value: "18:00" } });
    expect(time.checkValidity()).toBe(true);
  });

  it("commits native date input events before a subsequent time edit", () => {
    render(<RangeForm />);
    const date = screen.getByLabelText("Session starts date") as HTMLInputElement;
    date.value = "2026-10-08";
    fireEvent.input(date);
    fireEvent.change(screen.getByLabelText("Session starts time"), { target: { value: "23:30" } });
    const data = new FormData(screen.getByRole("form", { name: "Session dates" }) as HTMLFormElement);
    expect(data.get("startsAt")).toBe("2026-10-08T23:30");
    expect(data.get("endsAt")).toBe("2026-10-03T21:00");
  });
});
