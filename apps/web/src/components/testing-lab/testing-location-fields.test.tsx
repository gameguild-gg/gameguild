import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import type { TestingLocationSummary } from "@/lib/testing-lab/queries";
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

  it("starts in remote mode for a virtual location", () => {
    const location = {
      isVirtual: true,
      name: "Remote lab",
      virtualUrl: "https://meet.example.com/lab",
      status: "Maintenance",
      maxTestersCapacity: 12,
      maxProjectsCapacity: 3,
      contactEmail: "lab@example.com",
      contactPhone: "+1 555 0100",
      equipmentAvailable: "Remote capture kit",
      description: "Moderated online",
    } as TestingLocationSummary;
    render(<TestingLocationFields idPrefix="remote" location={location} />);

    expect(screen.getByLabelText("Meeting URL")).toHaveValue(location.virtualUrl);
    expect(screen.queryByLabelText("Street address")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Location name")).toHaveValue("Remote lab");
    expect(screen.getByLabelText("Tester capacity")).toHaveValue(12);
    expect(screen.getByLabelText("Project capacity")).toHaveValue(3);
    expect(screen.getByLabelText("Operations email")).toHaveValue("lab@example.com");
    expect(screen.getByLabelText("Operations phone")).toHaveValue("+1 555 0100");
    expect(screen.getByLabelText("Equipment and facilities")).toHaveValue("Remote capture kit");
    expect(screen.getByLabelText("Operating notes")).toHaveValue("Moderated online");
  });
});
