import "@testing-library/jest-dom/vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GroupsClient } from "./groups-client";
import {
  addGroupMember,
  createCourseGroup,
  createGroupSet,
  removeGroupMember,
} from "@/lib/learning/actions";

const routerMocks = vi.hoisted(() => ({
  back: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
}));

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

vi.mock("next/navigation", () => ({
  useRouter: () => routerMocks,
}));

vi.mock("@/lib/learning/actions", () => ({
  createGroupSet: vi.fn(),
  createCourseGroup: vi.fn(),
  addGroupMember: vi.fn(),
  removeGroupMember: vi.fn(),
}));

const sets = [
  {
    id: "set-1",
    name: "Project teams",
    groups: [
      {
        id: "group-1",
        name: "Team Alpha",
        capacity: 4,
        memberCount: 2,
        members: [
          { userId: "user-1", displayName: "Ada Lovelace" },
          { userId: "user-2", displayName: "Grace Hopper" },
        ],
      },
      {
        id: "group-2",
        name: "Team Beta",
        capacity: 3,
        memberCount: 0,
        members: [],
      },
    ],
  },
];

describe("GroupsClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(createGroupSet).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(createCourseGroup).mockResolvedValue({
      success: true,
      data: null,
    });
    vi.mocked(addGroupMember).mockResolvedValue({ success: true, data: null });
    vi.mocked(removeGroupMember).mockResolvedValue({
      success: true,
      data: null,
    });
  });

  it("renders group sets, groups, capacity, and member chips", () => {
    render(<GroupsClient courseId="course-1" sets={sets} />);

    expect(screen.getByText("Project teams")).toBeInTheDocument();
    expect(screen.getByText("Team Alpha")).toBeInTheDocument();
    expect(screen.getByText("Team Beta")).toBeInTheDocument();
    expect(screen.getByText("Ada Lovelace")).toBeInTheDocument();
    expect(screen.getByText("Grace Hopper")).toBeInTheDocument();
    expect(screen.getByText("2/4")).toBeInTheDocument();
    expect(screen.getByText("0/3")).toBeInTheDocument();
  });

  it("creates a group set", async () => {
    const user = userEvent.setup();
    render(<GroupsClient courseId="course-1" sets={sets} />);

    await user.type(
      screen.getByPlaceholderText(/new group set name/i),
      "Lab pairs",
    );
    await user.click(screen.getByRole("button", { name: /create set/i }));

    await waitFor(() => {
      expect(createGroupSet).toHaveBeenCalledWith("course-1", "Lab pairs");
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
  });

  it("blocks group creation while capacity is below 2 and creates once valid", async () => {
    const user = userEvent.setup();
    render(<GroupsClient courseId="course-1" sets={sets} />);

    const nameInput = screen.getAllByPlaceholderText(/group name/i)[0];
    const capacityInput = screen.getAllByLabelText(/^capacity$/i)[0];
    const addButton = screen.getAllByRole("button", { name: /add group/i })[0];

    await user.type(nameInput, "Team Gamma");
    await user.type(capacityInput, "1");

    expect(addButton).toBeDisabled();
    expect(createCourseGroup).not.toHaveBeenCalled();

    await user.clear(capacityInput);
    await user.type(capacityInput, "5");
    expect(addButton).toBeEnabled();

    await user.click(addButton);

    await waitFor(() => {
      expect(createCourseGroup).toHaveBeenCalledWith({
        courseId: "course-1",
        setId: "set-1",
        name: "Team Gamma",
        capacity: 5,
      });
    });
  });

  it("shows a validation message when capacity is below 2", async () => {
    const user = userEvent.setup();
    render(<GroupsClient courseId="course-1" sets={sets} />);

    const capacityInput = screen.getAllByLabelText(/^capacity$/i)[0];
    await user.type(capacityInput, "1");

    expect(screen.getByText(/capacity must be at least 2/i)).toBeInTheDocument();
  });

  it("adds a member by user reference and calls the action with the resolved payload", async () => {
    const user = userEvent.setup();
    render(<GroupsClient courseId="course-1" sets={sets} />);

    const memberInput = screen.getAllByPlaceholderText(
      /add member by email or user id/i,
    )[0];
    await user.type(memberInput, "ada@example.com");
    await user.click(screen.getAllByRole("button", { name: /add member/i })[0]);

    await waitFor(() => {
      expect(addGroupMember).toHaveBeenCalledWith({
        courseId: "course-1",
        groupId: "group-1",
        userReference: "ada@example.com",
      });
    });
  });

  it("removes a member via the chip close button", async () => {
    const user = userEvent.setup();
    render(<GroupsClient courseId="course-1" sets={sets} />);

    await user.click(
      screen.getByRole("button", { name: /remove member ada lovelace/i }),
    );

    await waitFor(() => {
      expect(removeGroupMember).toHaveBeenCalledWith({
        courseId: "course-1",
        groupId: "group-1",
        userId: "user-1",
      });
    });
  });

  it("surfaces action errors", async () => {
    const user = userEvent.setup();
    vi.mocked(createGroupSet).mockResolvedValueOnce({
      success: false,
      error: "Group set name already exists",
    });
    render(<GroupsClient courseId="course-1" sets={sets} />);

    await user.type(
      screen.getByPlaceholderText(/new group set name/i),
      "Project teams",
    );
    await user.click(screen.getByRole("button", { name: /create set/i }));

    expect(
      await screen.findByText("Group set name already exists"),
    ).toBeInTheDocument();
  });
});
