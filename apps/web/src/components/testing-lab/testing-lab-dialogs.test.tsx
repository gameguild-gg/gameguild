import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/testing-lab/actions", () => ({
  addTestingParticipant: vi.fn(),
  createTestingLabLocation: vi.fn(),
  createTestingLabRole: vi.fn(),
  createTestingSession: vi.fn(),
  linkTestingSessionProject: vi.fn(),
  submitTestingBuild: vi.fn(),
  updateTestingLabLocation: vi.fn(),
  updateTestingLabRole: vi.fn(),
  updateTestingRequest: vi.fn(),
  updateTestingSession: vi.fn(),
}));

import { CreateTestingLocationDialog } from "./testing-lab-dialogs";

describe("Testing Lab dialogs", () => {
  it("opens the location form from its composed trigger", () => {
    render(<CreateTestingLocationDialog />);

    fireEvent.click(screen.getByRole("button", { name: "New location" }));

    expect(
      screen.getByRole("dialog", { name: "Create testing location" }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("Location name")).toBeInTheDocument();
  });
});
