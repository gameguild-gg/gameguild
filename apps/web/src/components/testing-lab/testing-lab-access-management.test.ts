import { describe, expect, it, vi } from "vitest";
import {
  executeTestingLabAccessMutation,
  getTestingLabResourceActions,
} from "./testing-lab-access-management";

describe("TestingLabAccessManagement", () => {
  it("refreshes effective access after a successful access mutation", async () => {
    const operation = vi.fn(async () => ({
      success: true as const,
      data: null,
      message: "Assigned.",
    }));
    const inspect = vi.fn(async () => ({
      success: true as const,
      data: {
        userId: "user-1",
        assignedRoles: ["Facilitator"],
        permissions: { canViewSessions: true },
      },
      message: "Loaded.",
    }));

    const outcome = await executeTestingLabAccessMutation(operation, inspect, {
      userId: "user-1",
      roleName: "Facilitator",
    });

    expect(operation).toHaveBeenCalledOnce();
    expect(inspect).toHaveBeenCalledOnce();
    expect(outcome.effectiveAccess).toEqual(
      expect.objectContaining({ assignedRoles: ["Facilitator"] }),
    );
  });

  it("does not refresh effective access after a failed mutation", async () => {
    const operation = vi.fn(async () => ({
      success: false as const,
      error: "Assignment denied.",
    }));
    const inspect = vi.fn();

    const outcome = await executeTestingLabAccessMutation(operation, inspect, {
      roleName: "Facilitator",
    });

    expect(inspect).not.toHaveBeenCalled();
    expect(outcome).toEqual({
      result: { success: false, error: "Assignment denied." },
      effectiveAccess: null,
      refreshError: null,
    });
  });

  it("preserves a successful mutation when the effective-access refresh fails", async () => {
    const operation = vi.fn(async () => ({
      success: true as const,
      data: null,
      message: "Assigned.",
    }));
    const inspect = vi.fn(async () => ({
      success: false as const,
      error: "Inspection unavailable.",
    }));

    const outcome = await executeTestingLabAccessMutation(operation, inspect, {
      userId: "user-1",
      roleName: "Facilitator",
    });

    expect(outcome).toEqual({
      result: { success: true, data: null, message: "Assigned." },
      effectiveAccess: null,
      refreshError: "Inspection unavailable.",
    });
  });
  it("limits resource actions to operations supported by each resource type", () => {
    expect(getTestingLabResourceActions("TestingRequest")).toEqual([
      "read",
      "edit",
      "delete",
      "approve",
    ]);
    expect(getTestingLabResourceActions("TestingSession")).toEqual([
      "read",
      "edit",
      "delete",
    ]);
    expect(getTestingLabResourceActions("TestingLocation")).toEqual([
      "read",
      "edit",
      "delete",
    ]);
  });
});
