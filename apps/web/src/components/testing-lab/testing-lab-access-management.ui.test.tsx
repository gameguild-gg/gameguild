import "@testing-library/jest-dom/vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const actions = vi.hoisted(() => ({
  assignTestingLabRole: vi.fn(),
  grantTestingLabResourcePermission: vi.fn(),
  inspectTestingLabUserAccess: vi.fn(),
  revokeTestingLabResourcePermission: vi.fn(),
  revokeTestingLabRole: vi.fn(),
}));

vi.mock("@/lib/testing-lab/actions", () => actions);

import { TestingLabAccessManagement } from "./testing-lab-access-management";

const members = [
  { id: "user-1", label: "Alex Tester" },
  { id: "user-2", label: "Sam Reviewer" },
];

const roles = [
  {
    id: "role-1",
    name: "Facilitator",
    description: "Runs sessions",
    permissions: { canViewSessions: true },
  },
];

const resources = [
  { id: "request-1", label: "Vertical slice", type: "TestingRequest" as const },
  { id: "session-1", label: "Friday playtest", type: "TestingSession" as const },
  { id: "location-1", label: "Remote lab", type: "TestingLocation" as const },
];

function access(overrides: Record<string, unknown> = {}) {
  return {
    success: true as const,
    data: {
      userId: "user-1",
      assignedRoles: ["Facilitator"],
      permissions: { canViewSessions: true, canEditSessions: false },
      resourcePermissions: [],
      ...overrides,
    },
    message: "Effective Testing Lab access loaded.",
  };
}

async function choose(user: ReturnType<typeof userEvent.setup>, label: string, option: string) {
  await user.click(screen.getByLabelText(label));
  await user.click(await screen.findByRole("option", { name: option }));
}

describe("TestingLabAccessManagement UI", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    actions.inspectTestingLabUserAccess.mockResolvedValue(access());
    actions.assignTestingLabRole.mockResolvedValue({ success: true, data: null, message: "Role assigned." });
    actions.revokeTestingLabRole.mockResolvedValue({ success: true, data: null, message: "Role revoked." });
    actions.grantTestingLabResourcePermission.mockResolvedValue({ success: true, data: null, message: "Permission granted." });
    actions.revokeTestingLabResourcePermission.mockResolvedValue({ success: true, data: null, message: "Permission revoked." });
  });

  it("keeps management disabled until a member is selected and loads effective access when opened", async () => {
    const user = userEvent.setup();
    render(<TestingLabAccessManagement members={members} roles={roles} resources={resources} />);

    expect(screen.getByRole("button", { name: "Manage access" })).toBeDisabled();
    await choose(user, "Member", "Alex Tester");
    await user.click(screen.getByRole("button", { name: "Manage access" }));

    expect(await screen.findByRole("dialog", { name: /Testing Lab access · Alex Tester/ })).toBeInTheDocument();
    await waitFor(() => expect(actions.inspectTestingLabUserAccess).toHaveBeenCalledOnce());
    expect(screen.getAllByText("Facilitator")).not.toHaveLength(0);
    expect(screen.getByText("View Sessions")).toBeInTheDocument();
    expect(screen.queryByText("Edit Sessions")).not.toBeInTheDocument();
  });

  it("assigns and revokes a role, refreshing effective access after each mutation", async () => {
    const user = userEvent.setup();
    render(<TestingLabAccessManagement members={members} roles={roles} resources={resources} />);
    await choose(user, "Member", "Alex Tester");
    await user.click(screen.getByRole("button", { name: "Manage access" }));
    await screen.findAllByText("Facilitator");

    await choose(user, "Testing Lab role", "Facilitator");
    await user.click(screen.getByRole("button", { name: "Assign" }));
    await waitFor(() => expect(actions.assignTestingLabRole).toHaveBeenCalledOnce());
    expect(actions.inspectTestingLabUserAccess).toHaveBeenCalledTimes(2);

    const revokeRole = screen.getByRole("button", { name: "Revoke" });
    await waitFor(() => expect(revokeRole).toBeEnabled());
    await user.click(revokeRole);
    await waitFor(() => expect(actions.revokeTestingLabRole).toHaveBeenCalledOnce());
    expect(actions.inspectTestingLabUserAccess).toHaveBeenCalledTimes(3);
  });

  it("filters resources by type and grants then revokes a resource exception", async () => {
    const user = userEvent.setup();
    actions.inspectTestingLabUserAccess
      .mockResolvedValueOnce(access())
      .mockResolvedValueOnce(
        access({
          resourcePermissions: [
            {
              resourceType: "TestingSession",
              resourceId: "session-1",
              action: "edit",
              expiresAt: "2026-10-01T12:00:00.000Z",
            },
          ],
        }),
      )
      .mockResolvedValueOnce(access());

    render(<TestingLabAccessManagement members={members} roles={roles} resources={resources} />);
    await choose(user, "Member", "Alex Tester");
    await user.click(screen.getByRole("button", { name: "Manage access" }));
    await screen.findByText("Resource exceptions");

    await choose(user, "Resource type", "Session");
    await choose(user, "Action", "edit");
    await choose(user, "Resource", "Friday playtest");
    await user.click(screen.getByRole("button", { name: "Grant exception" }));

    await waitFor(() => expect(actions.grantTestingLabResourcePermission).toHaveBeenCalledOnce());
    expect(await screen.findAllByText("Friday playtest")).not.toHaveLength(0);
    expect(screen.getByText(/Expires/)).toBeInTheDocument();

    const revokePermission = screen.getByRole("button", { name: "Revoke resource permission" });
    await waitFor(() => expect(revokePermission).toBeEnabled());
    await user.click(revokePermission);
    await waitFor(() => expect(actions.revokeTestingLabResourcePermission).toHaveBeenCalledOnce());
    expect(actions.inspectTestingLabUserAccess).toHaveBeenCalledTimes(3);
  });

  it("shows mutation and refresh failures without discarding the saved operation result", async () => {
    const user = userEvent.setup();
    actions.assignTestingLabRole.mockResolvedValue({ success: true, data: null, message: "Role assigned." });
    actions.inspectTestingLabUserAccess
      .mockResolvedValueOnce(access({ assignedRoles: [], permissions: {}, resourcePermissions: [] }))
      .mockResolvedValueOnce({ success: false, error: "Access API unavailable" });

    render(<TestingLabAccessManagement members={members} roles={roles} resources={resources} />);
    await choose(user, "Member", "Alex Tester");
    await user.click(screen.getByRole("button", { name: "Manage access" }));
    await screen.findByText("No Testing Lab roles assigned.");
    await choose(user, "Testing Lab role", "Facilitator");
    await user.click(screen.getByRole("button", { name: "Assign" }));

    expect(
      await screen.findByText(
        "The change was saved, but effective access could not be refreshed: Access API unavailable",
      ),
    ).toBeInTheDocument();
  });

  it("clears stale access when another member is selected and supports manual refresh", async () => {
    const user = userEvent.setup();
    render(<TestingLabAccessManagement members={members} roles={roles} resources={resources} />);
    await choose(user, "Member", "Alex Tester");
    await user.click(screen.getByRole("button", { name: "Manage access" }));
    await screen.findByText("View Sessions");
    const refresh = screen.getByRole("button", { name: "Refresh access" });
    await waitFor(() => expect(refresh).toBeEnabled());
    await user.click(refresh);
    await waitFor(() => expect(actions.inspectTestingLabUserAccess).toHaveBeenCalledTimes(2));

    await user.click(screen.getByRole("button", { name: "Close" }));
    await choose(user, "Member", "Sam Reviewer");
    await user.click(screen.getByRole("button", { name: "Manage access" }));
    expect(await screen.findByRole("dialog", { name: /Testing Lab access · Sam Reviewer/ })).toBeInTheDocument();
  });
});
