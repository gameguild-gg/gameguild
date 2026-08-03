import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { TestingLocationFields } from "./testing-location-fields";

describe("TestingLocationFields", () => {
  it("switches between physical address and remote meeting details", async () => {
    render(<TestingLocationFields idPrefix="location" />);

    expect(screen.getByLabelText("Street address")).toBeVisible();
    expect(screen.queryByLabelText("Meeting URL")).not.toBeInTheDocument();

    await userEvent.click(screen.getByText("Remote"));

    expect(screen.getByLabelText("Meeting URL")).toBeVisible();
    expect(screen.queryByLabelText("Street address")).not.toBeInTheDocument();
  });
});
