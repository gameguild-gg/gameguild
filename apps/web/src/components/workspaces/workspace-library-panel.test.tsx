import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { WorkspaceLibraryPanel } from "./workspace-library-panel";

vi.mock("@/lib/workspaces", () => ({
  getWorkspaceAssetRevisions: vi.fn().mockResolvedValue([]),
}));

vi.mock("@/lib/workspace-actions", () => ({
  copyWorkspaceAssetForm: vi.fn(),
  createWorkspaceFolderForm: vi.fn(),
  restoreWorkspaceAssetRevisionForm: vi.fn(),
  restrictWorkspaceFolderForm: vi.fn(),
  updateProjectDeliverableUrlForm: vi.fn(),
  uploadWorkspaceAssetForm: vi.fn(),
}));

describe("WorkspaceLibraryPanel", () => {
  it("offers both file upload and an external project deliverable URL", async () => {
    const view = await WorkspaceLibraryPanel({
      title: "Project deliverables",
      library: { id: "library-1", folders: [], assets: [] },
      resourceType: "Project",
      resourceId: "project-1",
      returnPath: "/workspace/projects/project-1/files",
      externalUrl: "https://drive.google.com/file/d/build-1/view",
    });

    render(view);

    expect(screen.getByLabelText("Upload file")).toHaveAttribute(
      "type",
      "file",
    );
    expect(screen.getByLabelText("External deliverable URL")).toHaveValue(
      "https://drive.google.com/file/d/build-1/view",
    );
    expect(
      screen.getByRole("link", { name: "Open current link" }),
    ).toHaveAttribute("href", "https://drive.google.com/file/d/build-1/view");
  });

  it("does not expose project delivery controls for a Team library", async () => {
    const view = await WorkspaceLibraryPanel({
      title: "Team files",
      library: { id: "library-1", folders: [], assets: [] },
      resourceType: "Team",
      resourceId: "team-1",
      returnPath: "/workspace/teams/team-1/files",
    });

    render(view);

    expect(screen.getByLabelText("Upload file")).toBeInTheDocument();
    expect(
      screen.queryByLabelText("External deliverable URL"),
    ).not.toBeInTheDocument();
  });
});
