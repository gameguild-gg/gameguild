import { fireEvent, render, screen } from "@testing-library/react"
import { describe, expect, it, vi } from "vitest"

import { DateTimePicker } from "./date-time-picker"

describe("DateTimePicker time zones", () => {
  it("keeps the entered wall-clock time in the selected zone and rejects DST gaps", () => {
    const onValueChange = vi.fn()
    render(
      <DateTimePicker
        id="recurrence-end"
        name="recurrenceEndsAt"
        value="2026-03-08T01:30"
        onValueChange={onValueChange}
        timeZoneId="America/New_York"
        timezoneLabel="America/New_York"
        required
      />,
    )

    expect(screen.getByRole("button")).toHaveTextContent("01:30")
    fireEvent.click(screen.getByRole("button"))

    fireEvent.change(screen.getByLabelText("Hour"), {
      target: { value: "2" },
    })
    expect(screen.getByRole("alert")).toHaveTextContent(
      "does not exist in America/New_York",
    )
    expect(
      screen.getByRole("button", { name: "Apply date and time" }),
    ).toBeDisabled()

    fireEvent.change(screen.getByLabelText("Hour"), {
      target: { value: "3" },
    })
    fireEvent.click(screen.getByRole("button", { name: "Apply date and time" }))

    expect(onValueChange).toHaveBeenCalledWith("2026-03-08T03:30")
  })
})
